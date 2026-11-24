using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartStock.Core.Entities;
using SmartStock.Core.Interfaces;

namespace SmartStock.Infrastructure.Services;

public class AIEmailGeneratorService : IAIEmailGeneratorService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AIEmailGeneratorService> _logger;

    public AIEmailGeneratorService(
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<AIEmailGeneratorService> logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> GenerateUpcomingOrderEmailAsync(
        RecurringOrder order,
        Customer customer,
        string approvalPortalUrl,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";

        var itemsList = new StringBuilder();
        foreach (var item in order.Items)
        {
            itemsList.AppendLine($"• {item.Product?.Name ?? "Item"}: {item.Quantity} {item.Unit} @ ₹{item.UnitPrice:N2} = ₹{item.SubTotal:N2}");
        }

        string introText = string.Empty;

        if (!string.IsNullOrWhiteSpace(apiKey) && !apiKey.StartsWith("your_"))
        {
            try
            {
                var prompt = $@"You are a friendly grocery store manager for a trusted local Kirana and supermarket.
Generate a warm, professional, personalized email message for regular customer {customer.Name}.
Their scheduled recurring grocery delivery order #{order.OrderNumber} is upcoming.
Expected Delivery: {order.ExpectedDeliveryDate:dd MMMM yyyy} ({order.PreferredDeliveryTime ?? "Regular Delivery"}).
Delivery Address: {customer.DeliveryAddress}.
Order items:
{itemsList}
Total: ₹{order.TotalAmount:N2}.

Write a personalized greeting and 2-3 friendly sentences reminding them of their regular schedule, asking them to review or reply if they'd like to adjust quantities, add new items, or skip this cycle.
Do NOT output HTML tags, just polite plain text paragraphs.";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new { role = "user", parts = new object[] { new { text = prompt } } }
                    },
                    generationConfig = new
                    {
                        temperature = 0.4,
                        maxOutputTokens = 300
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(
                    $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}",
                    content,
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    var doc = JsonNode.Parse(responseJson);
                    introText = doc?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini email personalization failed. Falling back to built-in template.");
            }
        }

        if (string.IsNullOrWhiteSpace(introText))
        {
            introText = $"Hello {customer.Name},<br/><br/>Hope you are having a wonderful week! Your regular grocery delivery is scheduled for delivery on <strong>{order.ExpectedDeliveryDate:dd MMMM yyyy}</strong>. Here is your configured recurring order:";
        }
        else
        {
            introText = introText.Replace("\n\n", "<br/><br/>").Replace("\n", "<br/>");
        }

        var htmlBuilder = new StringBuilder();
        htmlBuilder.AppendLine("<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; color: #1e293b; line-height: 1.6; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden;'>");
        
        // Header
        htmlBuilder.AppendLine("<div style='background-color: #2563eb; color: #ffffff; padding: 24px; text-align: center;'>");
        htmlBuilder.AppendLine("<h2 style='margin: 0; font-size: 22px; font-weight: 700;'>SmartStock Grocery Delivery</h2>");
        htmlBuilder.AppendLine($"<p style='margin: 6px 0 0; font-size: 14px; opacity: 0.9;'>Upcoming Scheduled Order #{order.OrderNumber}</p>");
        htmlBuilder.AppendLine("</div>");

        // Body
        htmlBuilder.AppendLine("<div style='padding: 24px;'>");
        htmlBuilder.AppendLine($"<p style='font-size: 15px;'>{introText}</p>");

        // Items Table
        htmlBuilder.AppendLine("<table style='width: 100%; border-collapse: collapse; margin: 20px 0; font-size: 14px;'>");
        htmlBuilder.AppendLine("<thead><tr style='background-color: #f8fafc; border-bottom: 2px solid #e2e8f0; text-align: left;'>");
        htmlBuilder.AppendLine("<th style='padding: 10px 12px;'>Product</th>");
        htmlBuilder.AppendLine("<th style='padding: 10px 12px; text-align: center;'>Quantity</th>");
        htmlBuilder.AppendLine("<th style='padding: 10px 12px; text-align: right;'>Price</th>");
        htmlBuilder.AppendLine("<th style='padding: 10px 12px; text-align: right;'>Total</th>");
        htmlBuilder.AppendLine("</tr></thead><tbody>");

        foreach (var item in order.Items)
        {
            htmlBuilder.AppendLine("<tr style='border-bottom: 1px solid #f1f5f9;'>");
            htmlBuilder.AppendLine($"<td style='padding: 10px 12px; font-weight: 500;'>{item.Product?.Name ?? "Item"}</td>");
            htmlBuilder.AppendLine($"<td style='padding: 10px 12px; text-align: center;'>{item.Quantity} {item.Unit}</td>");
            htmlBuilder.AppendLine($"<td style='padding: 10px 12px; text-align: right;'>₹{item.UnitPrice:N2}</td>");
            htmlBuilder.AppendLine($"<td style='padding: 10px 12px; text-align: right; font-weight: 600;'>₹{item.SubTotal:N2}</td>");
            htmlBuilder.AppendLine("</tr>");
        }

        htmlBuilder.AppendLine("</tbody><tfoot>");
        htmlBuilder.AppendLine("<tr style='border-top: 2px solid #e2e8f0;'>");
        htmlBuilder.AppendLine("<td colspan='3' style='padding: 12px; font-weight: 700; text-align: right;'>Grand Total:</td>");
        htmlBuilder.AppendLine($"<td style='padding: 12px; font-weight: 700; text-align: right; color: #2563eb; font-size: 16px;'>₹{order.TotalAmount:N2}</td>");
        htmlBuilder.AppendLine("</tr></tfoot></table>");

        // Delivery details box
        htmlBuilder.AppendLine("<div style='background-color: #f1f5f9; padding: 14px; border-radius: 6px; margin-bottom: 24px; font-size: 13px;'>");
        htmlBuilder.AppendLine($"<strong>Expected Delivery:</strong> {order.ExpectedDeliveryDate:dd MMMM yyyy} ({order.PreferredDeliveryTime ?? "Morning 09:00 - 12:00"})<br/>");
        htmlBuilder.AppendLine($"<strong>Delivery Address:</strong> {customer.DeliveryAddress}");
        if (!string.IsNullOrWhiteSpace(order.SpecialInstructions))
        {
            htmlBuilder.AppendLine($"<br/><strong>Special Instructions:</strong> {order.SpecialInstructions}");
        }
        htmlBuilder.AppendLine("</div>");

        // Action CTA
        htmlBuilder.AppendLine("<div style='text-align: center; margin: 30px 0;'>");
        htmlBuilder.AppendLine($"<a href='{approvalPortalUrl}' style='background-color: #16a34a; color: #ffffff; padding: 14px 28px; text-decoration: none; border-radius: 6px; font-weight: 600; font-size: 15px; display: inline-block;'>Review & Confirm Order</a>");
        htmlBuilder.AppendLine("</div>");

        // Natural Language Reply Instructions
        htmlBuilder.AppendLine("<div style='background-color: #eff6ff; border: 1px solid #bfdbfe; border-radius: 6px; padding: 14px; font-size: 13px; color: #1e3a8a;'>");
        htmlBuilder.AppendLine("<strong>💬 Or simply reply directly to this email:</strong><br/>");
        htmlBuilder.AppendLine("• <em>\"Yes, send me the same order.\"</em><br/>");
        htmlBuilder.AppendLine("• <em>\"Send 15 kg rice instead of 10 kg and 3 litres oil.\"</em><br/>");
        htmlBuilder.AppendLine("• <em>\"Please skip this delivery.\"</em><br/>");
        htmlBuilder.AppendLine("• <em>\"Send it next Monday instead.\"</em>");
        htmlBuilder.AppendLine("</div>");

        htmlBuilder.AppendLine("</div>"); // padding: 24px

        // Footer
        htmlBuilder.AppendLine("<div style='background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 16px; text-align: center; font-size: 12px; color: #64748b;'>");
        htmlBuilder.AppendLine($"SmartStock Automated Recurring Services • Order #{order.OrderNumber}<br/>");
        htmlBuilder.AppendLine("Thank you for being a valued customer!");
        htmlBuilder.AppendLine("</div></div>");

        return htmlBuilder.ToString();
    }

    public async Task<string> GenerateOrderConfirmationEmailAsync(
        RecurringOrder order,
        Customer customer,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        var html = $@"<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; border: 1px solid #e2e8f0; border-radius: 8px;'>
<h2 style='color: #16a34a;'>Order Confirmed!</h2>
<p>Hello {customer.Name},</p>
<p>Your recurring grocery order <strong>#{order.OrderNumber}</strong> has been confirmed and is being prepared for delivery.</p>
<div style='background-color: #f8fafc; padding: 12px; border-radius: 6px; margin: 16px 0;'>
<p><strong>Scheduled Delivery:</strong> {order.ExpectedDeliveryDate:dd MMMM yyyy} ({order.PreferredDeliveryTime ?? "Regular Delivery"})</p>
<p><strong>Delivery Address:</strong> {customer.DeliveryAddress}</p>
<p><strong>Total Amount:</strong> ₹{order.TotalAmount:N2}</p>
</div>
<p>Thank you for shopping with us!</p>
</div>";
        return html;
    }

    public async Task<string> GenerateOrderSkippedEmailAsync(
        RecurringOrder order,
        Customer customer,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        var html = $@"<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; border: 1px solid #e2e8f0; border-radius: 8px;'>
<h2 style='color: #64748b;'>Delivery Skipped</h2>
<p>Hello {customer.Name},</p>
<p>As requested, your scheduled delivery for <strong>{order.ExpectedDeliveryDate:dd MMMM yyyy}</strong> (Order #{order.OrderNumber}) has been skipped. No order will be dispatched and you will not be charged.</p>
<p>Your next recurring delivery cycle remains active as configured.</p>
<p>Thank you,<br/>SmartStock Team</p>
</div>";
        return html;
    }
}
