using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Exceptions;
using SmartStock.Core.Interfaces;

namespace SmartStock.Infrastructure.AI;

/// <summary>
/// Extracts structured invoice data from an uploaded image or PDF using Gemini.
/// Never fabricates data: if extraction cannot be completed, an <see cref="AppException"/>
/// with a user-readable reason is thrown so nothing fake reaches review or inventory.
/// </summary>
public class InvoiceScannerService : IInvoiceScannerService
{
    public const string DefaultModel = "gemini-3.8-flash";

    private const int MaxAttempts = 3;
    private static readonly HttpStatusCode[] TransientStatusCodes =
    [
        HttpStatusCode.TooManyRequests,
        HttpStatusCode.InternalServerError,
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout,
    ];

    private static readonly Dictionary<string, string> MimeTypesByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".webp"] = "image/webp",
        [".heic"] = "image/heic",
        [".heif"] = "image/heif",
    };

    private static readonly JsonSerializerOptions ExtractionJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new LenientDecimalConverter(), new LenientNullableDateTimeConverter() },
    };

    private const string Prompt =
        "You are reading a supplier purchase invoice / bill (it may be printed, handwritten, photographed or a PDF; " +
        "it may be in English or an Indian language). Extract exactly what is written on THIS document.\n" +
        "Rules:\n" +
        "- Never invent values. If a field is not present on the document, use null for text/date fields and 0 for amounts.\n" +
        "- Amounts are plain numbers without currency symbols or thousands separators (\"Rs. 1,200.50\" -> 1200.5).\n" +
        "- Dates are YYYY-MM-DD. Indian invoices usually write dates as DD/MM/YYYY.\n" +
        "- Include every line item row. quantity, unitPrice and lineTotal are per row.\n" +
        "- supplierName/supplierGst describe the seller who issued the invoice, not the buyer.\n" +
        "- If the document is not an invoice or bill, return an empty items list and null invoiceNumber.";

    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<InvoiceScannerService> _logger;

    public InvoiceScannerService(
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<InvoiceScannerService> logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ApiResponse<InvoiceExtractResponseDto>> ExtractInvoiceAsync(
        Stream imageStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (imageStream == null || imageStream.Length == 0)
        {
            throw new ValidationAppException("File", "An invoice document or image must be provided.");
        }

        var mimeType = ResolveMimeType(fileName, contentType)
            ?? throw new ValidationAppException(
                "File", "Unsupported file type. Please upload a PDF, JPG, PNG, WEBP or HEIC invoice.");

        var apiKey = _configuration["Gemini:ApiKey"]?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("your_", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException(
                "AI invoice scanning is not configured. Set your Gemini API key in the backend configuration (Gemini:ApiKey).",
                503);
        }

        var model = string.IsNullOrWhiteSpace(_configuration["Gemini:Model"])
            ? DefaultModel
            : _configuration["Gemini:Model"]!.Trim();

        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream, cancellationToken);

        var requestJson = BuildRequestJson(Convert.ToBase64String(memoryStream.ToArray()), mimeType, model);
        var responseJson = await SendWithRetryAsync(model, apiKey, requestJson, fileName, cancellationToken);

        var extracted = ParseExtraction(responseJson, fileName);

        _logger.LogInformation(
            "Gemini ({Model}) extracted invoice {InvoiceNumber} from {FileName}: {ItemCount} line items, total {Total}.",
            model, extracted.InvoiceNumber, fileName, extracted.Items.Count, extracted.TotalAmount);

        return ApiResponse<InvoiceExtractResponseDto>.Ok(
            extracted, "Invoice extracted successfully. Please review before saving purchase.");
    }

    private string BuildRequestJson(string base64Data, string mimeType, string model)
    {
        var generationConfig = new JsonObject
        {
            ["temperature"] = 0.1,
            ["responseMimeType"] = "application/json",
            ["responseSchema"] = BuildResponseSchema(),
        };

        // Gemini 3+ models think by default, which is slow for plain data extraction.
        // Gemini 2.x models use a different setting (thinkingBudget), so leave them untouched.
        var thinkingLevel = _configuration["Gemini:InvoiceThinkingLevel"] ?? "low";
        if (!string.IsNullOrWhiteSpace(thinkingLevel) && !model.StartsWith("gemini-2", StringComparison.OrdinalIgnoreCase))
        {
            generationConfig["thinkingConfig"] = new JsonObject { ["thinkingLevel"] = thinkingLevel.Trim() };
        }

        var body = new JsonObject
        {
            ["contents"] = new JsonArray
            {
                new JsonObject
                {
                    ["parts"] = new JsonArray
                    {
                        new JsonObject { ["inlineData"] = new JsonObject { ["mimeType"] = mimeType, ["data"] = base64Data } },
                        new JsonObject { ["text"] = Prompt },
                    },
                },
            },
            ["generationConfig"] = generationConfig,
        };

        return body.ToJsonString();
    }

    /// <summary>Structured-output schema so Gemini returns typed numbers and real nulls.</summary>
    private static JsonObject BuildResponseSchema()
    {
        static JsonObject Str(bool nullable = true) => new() { ["type"] = "STRING", ["nullable"] = nullable };
        static JsonObject Num() => new() { ["type"] = "NUMBER" };

        var item = new JsonObject
        {
            ["type"] = "OBJECT",
            ["properties"] = new JsonObject
            {
                ["productName"] = Str(nullable: false),
                ["description"] = Str(),
                ["supplierSku"] = Str(),
                ["hsnCode"] = Str(),
                ["quantity"] = Num(),
                ["unit"] = new JsonObject
                {
                    ["type"] = "STRING",
                    ["nullable"] = true,
                    ["description"] = "Unit as written, e.g. PCS, KG, G, LITRE, ML, BOX, PACKET, BAG, DOZEN",
                },
                ["unitPrice"] = Num(),
                ["discount"] = Num(),
                ["taxPercentage"] = Num(),
                ["taxAmount"] = Num(),
                ["lineTotal"] = Num(),
            },
            ["required"] = new JsonArray("productName", "quantity", "unitPrice", "lineTotal"),
        };

        return new JsonObject
        {
            ["type"] = "OBJECT",
            ["properties"] = new JsonObject
            {
                ["supplierName"] = Str(),
                ["supplierGst"] = Str(),
                ["invoiceNumber"] = Str(),
                ["invoiceDate"] = new JsonObject { ["type"] = "STRING", ["nullable"] = true, ["description"] = "YYYY-MM-DD" },
                ["dueDate"] = new JsonObject { ["type"] = "STRING", ["nullable"] = true, ["description"] = "YYYY-MM-DD" },
                ["paymentTerms"] = Str(),
                ["billingAddress"] = Str(),
                ["deliveryAddress"] = Str(),
                ["purchaseOrderNumber"] = Str(),
                ["subTotal"] = Num(),
                ["discount"] = Num(),
                ["taxableAmount"] = Num(),
                ["cgst"] = Num(),
                ["sgst"] = Num(),
                ["igst"] = Num(),
                ["otherCharges"] = Num(),
                ["totalAmount"] = Num(),
                ["items"] = new JsonObject { ["type"] = "ARRAY", ["items"] = item },
            },
            ["required"] = new JsonArray("supplierName", "invoiceNumber", "invoiceDate", "totalAmount", "items"),
        };
    }

    private async Task<string> SendWithRetryAsync(
        string model, string apiKey, string requestJson, string fileName, CancellationToken cancellationToken)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent";

        for (var attempt = 1; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json"),
            };
            // Header instead of ?key= so the key never appears in request URLs or logs.
            request.Headers.Add("x-goog-api-key", apiKey);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // A timeout means the model is slow, not overloaded; retrying would only multiply the wait.
                _logger.LogWarning("Gemini ({Model}) timed out for {FileName}.", model, fileName);
                throw new AppException(
                    "The AI service took too long to read this invoice. Please try again, or upload a clearer / smaller file.", 504);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Could not reach Gemini for {FileName}.", fileName);
                throw new AppException("Could not reach the AI service. Check the server's internet connection and try again.", 502);
            }

            using (response)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return body;
                }

                var googleMessage = ReadGoogleErrorMessage(body);
                _logger.LogWarning(
                    "Gemini ({Model}) returned {StatusCode} on attempt {Attempt} for {FileName}: {Message}",
                    model, (int)response.StatusCode, attempt, fileName, googleMessage);

                if (TransientStatusCodes.Contains(response.StatusCode) && attempt < MaxAttempts)
                {
                    await Task.Delay(RetryDelay(response, attempt), cancellationToken);
                    continue;
                }

                throw response.StatusCode switch
                {
                    HttpStatusCode.TooManyRequests => new AppException(
                        $"Gemini API quota or rate limit reached. Please wait a minute and try again. ({googleMessage})", 429),
                    HttpStatusCode.ServiceUnavailable or HttpStatusCode.InternalServerError
                        or HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout => new AppException(
                        $"The Gemini AI service is temporarily unavailable. Please try again in a few minutes. ({googleMessage})", 503),
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new AppException(
                        $"The Gemini API key was rejected. Check Gemini:ApiKey. ({googleMessage})", 502),
                    HttpStatusCode.NotFound => new AppException(
                        $"Gemini model '{model}' is not available for this API key. Update Gemini:Model. ({googleMessage})", 502),
                    _ => new AppException($"The AI service could not process this invoice. ({googleMessage})", 502),
                };
            }
        }
    }

    private static TimeSpan RetryDelay(HttpResponseMessage response, int attempt)
    {
        var retryAfter = response.Headers.RetryAfter?.Delta;
        if (retryAfter is { } delta && delta > TimeSpan.Zero && delta <= TimeSpan.FromSeconds(20))
        {
            return delta;
        }
        return TimeSpan.FromSeconds(attempt == 1 ? 2 : 6);
    }

    private InvoiceExtractResponseDto ParseExtraction(string responseJson, string fileName)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(responseJson);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Gemini returned a non-JSON response for {FileName}.", fileName);
            throw new AppException("The AI service returned an unreadable response. Please try again.", 502);
        }

        var blockReason = root?["promptFeedback"]?["blockReason"]?.GetValue<string>();
        if (!string.IsNullOrEmpty(blockReason))
        {
            throw new AppException($"The AI service refused to read this file ({blockReason}).", 422);
        }

        var candidate = root?["candidates"]?[0];
        var finishReason = candidate?["finishReason"]?.GetValue<string>();

        // Join all non-thought text parts (thinking models may split output into several parts).
        var text = string.Concat(
            (candidate?["content"]?["parts"]?.AsArray() ?? new JsonArray())
                .Where(p => p?["thought"]?.GetValue<bool>() != true)
                .Select(p => p?["text"]?.GetValue<string>() ?? string.Empty));

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Gemini returned no text for {FileName}. finishReason={FinishReason}", fileName, finishReason);
            throw new AppException(
                $"The AI service returned no data for this invoice{(finishReason is null ? "" : $" ({finishReason})")}. Please try again.", 502);
        }

        var cleaned = StripCodeFence(text);

        InvoiceExtractResponseDto? extracted;
        try
        {
            extracted = JsonSerializer.Deserialize<InvoiceExtractResponseDto>(cleaned, ExtractionJsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Could not parse Gemini invoice JSON for {FileName}. finishReason={FinishReason}. Output: {Output}",
                fileName, finishReason, cleaned.Length > 2000 ? cleaned[..2000] : cleaned);
            throw new AppException(
                finishReason == "MAX_TOKENS"
                    ? "This invoice is too long for a single scan. Try uploading fewer pages at a time."
                    : "The AI service returned invoice data in an unexpected format. Please try again.",
                502);
        }

        if (extracted == null)
        {
            throw new AppException("The AI service returned no invoice data. Please try again.", 502);
        }

        extracted.Items = (extracted.Items ?? [])
            .Where(i => i != null && !string.IsNullOrWhiteSpace(i.ProductName))
            .ToList();
        foreach (var item in extracted.Items)
        {
            item.ProductName = item.ProductName.Trim();
            item.Unit = string.IsNullOrWhiteSpace(item.Unit) ? "PCS" : item.Unit.Trim();
        }

        if (extracted.Items.Count == 0 && string.IsNullOrWhiteSpace(extracted.InvoiceNumber) && extracted.TotalAmount == 0)
        {
            throw new AppException(
                "No invoice details could be read from this file. Make sure it is a clear photo or PDF of a supplier invoice.", 422);
        }

        extracted.RawExtractedJson = cleaned;
        extracted.Notes = "Extracted automatically by SmartStock AI. Verify prices, quantities, and matched products before confirming.";
        return extracted;
    }

    private static string? ResolveMimeType(string fileName, string? contentType)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty);
        if (MimeTypesByExtension.TryGetValue(extension, out var byExtension))
        {
            return byExtension;
        }

        var normalized = contentType?.Split(';')[0].Trim().ToLowerInvariant();
        if (normalized == "image/jpg")
        {
            return "image/jpeg";
        }
        return normalized is not null && MimeTypesByExtension.ContainsValue(normalized) ? normalized : null;
    }

    private static string ReadGoogleErrorMessage(string body)
    {
        try
        {
            var message = JsonNode.Parse(body)?["error"]?["message"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(message))
            {
                return message.Length > 300 ? message[..300] + "…" : message;
            }
        }
        catch (JsonException)
        {
            // Fall through to a generic message.
        }
        return "no details returned";
    }

    private static string StripCodeFence(string text)
    {
        var cleaned = text.Trim();
        if (cleaned.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewLine = cleaned.IndexOf('\n');
            cleaned = firstNewLine >= 0 ? cleaned[(firstNewLine + 1)..] : cleaned[3..];
        }
        if (cleaned.EndsWith("```", StringComparison.Ordinal))
        {
            cleaned = cleaned[..^3];
        }
        return cleaned.Trim();
    }

    /// <summary>Accepts numbers, numeric strings ("1,200.50", "Rs. 450", "18%") and null (as 0).</summary>
    private sealed class LenientDecimalConverter : JsonConverter<decimal>
    {
        private static readonly Regex NumberPattern = new(@"-?\d[\d,]*(?:\.\d+)?", RegexOptions.Compiled);

        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Number:
                    return reader.TryGetDecimal(out var number) ? number : (decimal)reader.GetDouble();
                case JsonTokenType.Null:
                    return 0m;
                case JsonTokenType.String:
                    // Take the first number in the text so "Rs. 450" is 450, not ".450".
                    var match = NumberPattern.Match(reader.GetString() ?? string.Empty);
                    return match.Success && decimal.TryParse(match.Value.Replace(",", string.Empty),
                        NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                        ? parsed
                        : 0m;
                default:
                    throw new JsonException($"Unexpected token {reader.TokenType} for a numeric field.");
            }
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value);
    }

    /// <summary>Accepts ISO and common Indian date formats; unreadable dates become null instead of failing.</summary>
    private sealed class LenientNullableDateTimeConverter : JsonConverter<DateTime?>
    {
        private static readonly string[] Formats =
        [
            "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ", "yyyy-MM-ddTHH:mm:ss.fffZ",
            "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "dd.MM.yyyy", "d.M.yyyy",
            "dd/MM/yy", "d/M/yy", "dd-MMM-yyyy", "d-MMM-yyyy", "dd MMM yyyy", "d MMM yyyy", "MMM d, yyyy",
        ];

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                reader.Skip();
                return null;
            }

            var value = reader.GetString()?.Trim();
            if (string.IsNullOrEmpty(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return DateTime.TryParseExact(value, Formats, CultureInfo.InvariantCulture,
                       DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed)
                ? DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc)
                : null;
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue) writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            else writer.WriteNullValue();
        }
    }
}
