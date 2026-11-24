using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SmartStock.Core.Exceptions;
using SmartStock.Infrastructure.AI;
using Xunit;

namespace SmartStock.Tests;

public class InvoiceScannerServiceTests
{
    private const string FakeKey = "test-api-key";

    /// <summary>Returns queued responses in order and records every request.</summary>
    private sealed class StubHandler(params Func<HttpResponseMessage>[] responses) : HttpMessageHandler
    {
        private int _next;
        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            Bodies.Add(request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
            return responses[Math.Min(_next++, responses.Length - 1)]();
        }
    }

    private static InvoiceScannerService CreateService(StubHandler handler, string? apiKey = FakeKey, string? model = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gemini:ApiKey"] = apiKey,
                ["Gemini:Model"] = model,
            })
            .Build();
        return new InvoiceScannerService(config, new HttpClient(handler), NullLogger<InvoiceScannerService>.Instance);
    }

    private static MemoryStream FakeFile() => new(Encoding.UTF8.GetBytes("%PDF-1.4 fake invoice bytes"));

    private static Func<HttpResponseMessage> GeminiReturns(string invoiceJson) => () =>
        new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                candidates = new[]
                {
                    new { content = new { parts = new[] { new { text = invoiceJson } } }, finishReason = "STOP" },
                },
            }), Encoding.UTF8, "application/json"),
        };

    private static Func<HttpResponseMessage> GeminiError(HttpStatusCode status, string message) => () =>
        new HttpResponseMessage(status)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { error = new { code = (int)status, message } }),
                Encoding.UTF8, "application/json"),
        };

    [Fact]
    public async Task Extract_ReturnsTheInvoiceGeminiRead_WithLenientValues()
    {
        // Values Gemini commonly returns that used to break strict parsing and trigger the fake fallback.
        const string json = """
        {
          "supplierName": "Shree Ganesh Traders",
          "supplierGst": "27ABCDE1234F1Z5",
          "invoiceNumber": "SGT/2311/0457",
          "invoiceDate": "18/11/2026",
          "dueDate": null,
          "subTotal": "8,060.00",
          "discount": null,
          "cgst": 315.9,
          "sgst": 315.9,
          "igst": null,
          "totalAmount": "Rs. 8,691.80",
          "items": [
            { "productName": "Parle-G Biscuit 800g", "quantity": 24, "unit": "PKT", "unitPrice": "Rs. 82", "taxPercentage": "18%", "lineTotal": 1968 },
            { "productName": "Aashirvaad Atta 10kg", "quantity": 10, "unit": null, "unitPrice": 455, "discount": null, "lineTotal": 4550 },
            { "productName": "  ", "quantity": 0, "unitPrice": 0, "lineTotal": 0 }
          ]
        }
        """;
        var handler = new StubHandler(GeminiReturns(json));

        var result = await CreateService(handler).ExtractInvoiceAsync(FakeFile(), "bill.pdf", "application/pdf");

        var invoice = result.Data!;
        invoice.SupplierName.Should().Be("Shree Ganesh Traders");
        invoice.InvoiceNumber.Should().Be("SGT/2311/0457");
        invoice.InvoiceDate.Should().Be(new DateTime(2026, 11, 18));
        invoice.DueDate.Should().BeNull();
        invoice.SubTotal.Should().Be(8060.00m);
        invoice.Discount.Should().Be(0m);
        invoice.TotalAmount.Should().Be(8691.80m);
        invoice.Items.Should().HaveCount(2, "rows without a product name are dropped");
        invoice.Items[0].UnitPrice.Should().Be(82m);
        invoice.Items[0].TaxPercentage.Should().Be(18m);
        invoice.Items[1].Unit.Should().Be("PCS");
        invoice.RawExtractedJson.Should().Contain("Shree Ganesh Traders");
    }

    [Fact]
    public async Task Extract_SendsKeyInHeader_AndUsesConfiguredModel()
    {
        var handler = new StubHandler(GeminiReturns("""{"invoiceNumber":"A-1","totalAmount":10,"items":[]}"""));

        await CreateService(handler, model: "gemini-test-model").ExtractInvoiceAsync(FakeFile(), "bill.png", "image/png");

        var request = handler.Requests.Single();
        request.RequestUri!.ToString().Should().Contain("models/gemini-test-model:generateContent").And.NotContain(FakeKey);
        request.Headers.GetValues("x-goog-api-key").Should().ContainSingle().Which.Should().Be(FakeKey);
        handler.Bodies.Single().Should().Contain("\"mimeType\":\"image/png\"").And.Contain("responseSchema");
    }

    [Fact]
    public async Task Extract_RetriesTemporaryOverload_ThenSucceeds()
    {
        var handler = new StubHandler(
            GeminiError(HttpStatusCode.ServiceUnavailable, "This model is currently experiencing high demand."),
            GeminiReturns("""{"invoiceNumber":"RETRY-OK","totalAmount":99,"items":[]}"""));

        var result = await CreateService(handler).ExtractInvoiceAsync(FakeFile(), "bill.jpg", "image/jpeg");

        result.Data!.InvoiceNumber.Should().Be("RETRY-OK");
        handler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task Extract_RetiredModel_ThrowsClearErrorInsteadOfSampleData()
    {
        var handler = new StubHandler(GeminiError(HttpStatusCode.NotFound,
            "This model models/gemini-2.5-flash is no longer available to new users."));

        var act = () => CreateService(handler, model: "gemini-2.5-flash").ExtractInvoiceAsync(FakeFile(), "bill.pdf", "application/pdf");

        var error = (await act.Should().ThrowAsync<AppException>()).Which;
        error.Message.Should().Contain("gemini-2.5-flash").And.Contain("no longer available");
        error.Message.Should().NotContain("Metro Cash");
        handler.Requests.Should().HaveCount(1, "a 404 is not retried");
    }

    [Fact]
    public async Task Extract_WithoutApiKey_ThrowsNotConfigured_AndCallsNothing()
    {
        var handler = new StubHandler(GeminiReturns("{}"));

        var act = () => CreateService(handler, apiKey: "").ExtractInvoiceAsync(FakeFile(), "bill.pdf", "application/pdf");

        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(503);
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Extract_DocumentWithNothingReadable_IsRejected()
    {
        var handler = new StubHandler(GeminiReturns("""{"supplierName":null,"invoiceNumber":null,"totalAmount":0,"items":[]}"""));

        var act = () => CreateService(handler).ExtractInvoiceAsync(FakeFile(), "holiday-photo.jpg", "image/jpeg");

        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(422);
    }

    [Fact]
    public async Task Extract_UnsupportedFileType_IsRejectedBeforeCallingGemini()
    {
        var handler = new StubHandler(GeminiReturns("{}"));

        var act = () => CreateService(handler).ExtractInvoiceAsync(FakeFile(), "invoice.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        await act.Should().ThrowAsync<ValidationAppException>();
        handler.Requests.Should().BeEmpty();
    }
}
