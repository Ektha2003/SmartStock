using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Entities;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Data;

namespace SmartStock.Infrastructure.AI;

public class AIAssistantService : IAIAssistantService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AIAssistantService> _logger;

    public AIAssistantService(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<AIAssistantService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ApiResponse<AIAssistantResponseDto>> AskAssistantAsync(AIAssistantRequestDto request, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var question = request.Question?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(question))
        {
            return ApiResponse<AIAssistantResponseDto>.Fail("Question cannot be empty.", "question", "Please enter a valid question.");
        }

        // 1. Fetch compact, business-isolated context
        var business = await _context.Businesses.FindAsync([businessId], cancellationToken);
        var businessName = business?.BusinessName ?? "Your Store";

        var today = DateTime.UtcNow.Date;
        var startOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var last7Days = today.AddDays(-7);

        var totalSalesToday = await _context.Sales
            .Where(s => s.BusinessId == businessId && s.SaleDate >= today)
            .SumAsync(s => (decimal?)s.TotalAmount, cancellationToken) ?? 0;

        var totalSalesMonth = await _context.Sales
            .Where(s => s.BusinessId == businessId && s.SaleDate >= startOfMonth)
            .SumAsync(s => (decimal?)s.TotalAmount, cancellationToken) ?? 0;

        var products = await _context.Products
            .Include(p => p.Category)
            .Where(p => p.BusinessId == businessId && p.IsActive)
            .ToListAsync(cancellationToken);

        var totalStockValue = products.Sum(p => p.CurrentStock * p.PurchasePrice);
        var lowStockProducts = products.Where(p => p.CurrentStock <= p.MinimumStock).ToList();
        var outOfStockProducts = products.Where(p => p.CurrentStock <= 0).ToList();

        // Top 5 sellers in last 30 days
        var topSellers = await _context.SaleItems
            .Include(si => si.Product)
            .Where(si => si.Product.BusinessId == businessId)
            .GroupBy(si => si.Product.Name)
            .Select(g => new { Name = g.Key, Qty = g.Sum(x => x.Quantity), Revenue = g.Sum(x => x.SubTotal) })
            .OrderByDescending(x => x.Revenue)
            .Take(5)
            .ToListAsync(cancellationToken);

        var apiKey = _configuration["Gemini:ApiKey"];
        var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";
        if (model.Contains("3.8", StringComparison.OrdinalIgnoreCase)) model = "gemini-2.5-flash";

        // If Gemini API Key is missing or unavailable, provide intelligent deterministic fallback
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("your_"))
        {
            _logger.LogInformation("Gemini API key is not configured. Serving deterministic assistant fallback.");
            var fallback = GenerateDeterministicFallback(question, businessName, products.Count, totalStockValue, totalSalesToday, totalSalesMonth, lowStockProducts, outOfStockProducts, topSellers);
            return ApiResponse<AIAssistantResponseDto>.Ok(fallback, "Response generated via built-in SmartStock intelligence.");
        }

        try
        {
            // Build compact prompt context
            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine($"Store Name: {businessName}");
            contextBuilder.AppendLine($"Total Active Products: {products.Count}");
            contextBuilder.AppendLine($"Total Inventory Cost Value: Rs. {totalStockValue:N2}");
            contextBuilder.AppendLine($"Sales Today: Rs. {totalSalesToday:N2}");
            contextBuilder.AppendLine($"Sales This Month: Rs. {totalSalesMonth:N2}");
            contextBuilder.AppendLine($"Out of Stock Products ({outOfStockProducts.Count}): " + 
                (outOfStockProducts.Count > 0 ? string.Join(", ", outOfStockProducts.Take(10).Select(p => p.Name)) : "None"));
            contextBuilder.AppendLine($"Low Stock Products ({lowStockProducts.Count}): " + 
                (lowStockProducts.Count > 0 ? string.Join(", ", lowStockProducts.Take(10).Select(p => $"{p.Name} ({p.CurrentStock}/{p.MinimumStock} {p.Unit})")) : "None"));
            contextBuilder.AppendLine("Top Selling Products: " + 
                string.Join(", ", topSellers.Select(t => $"{t.Name} ({t.Qty} sold, Rs. {t.Revenue:N0})")));

            var systemPrompt = 
                "You are SmartStock AI, an expert business advisor for Indian Kirana stores and small retailers. " +
                "Answer the shopkeeper's question concisely, practically, and politely using the store's verified metrics provided below. " +
                "All monetary values are in Indian Rupees (INR / Rs.). Give actionable stock and sales advice. " +
                "Do NOT make up facts not in the context. Never advise dangerous or illegal modifications. " +
                "Keep responses under 150 words unless detailed analysis is asked.\n\n" +
                "STORE METRICS:\n" + contextBuilder;

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = $"{systemPrompt}\n\nUSER QUESTION: {question}" }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    maxOutputTokens = 600
                }
            };

            var jsonPayload = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Attempt primary model first, fallback to gemini-1.5-flash if needed
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
            var httpResponse = await _httpClient.PostAsync(url, content, cancellationToken);

            if (!httpResponse.IsSuccessStatusCode && model != "gemini-1.5-flash")
            {
                _logger.LogInformation("Gemini {Model} returned {StatusCode}. Retrying with gemini-1.5-flash fallback.", model, httpResponse.StatusCode);
                var fallbackUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
                var retryContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                httpResponse = await _httpClient.PostAsync(fallbackUrl, retryContent, cancellationToken);
            }

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Gemini API call failed with status {StatusCode}: {ErrorBody}. Serving deterministic fallback.", httpResponse.StatusCode, errorBody);

                var fallback = GenerateDeterministicFallback(question, businessName, products.Count, totalStockValue, totalSalesToday, totalSalesMonth, lowStockProducts, outOfStockProducts, topSellers);
                return ApiResponse<AIAssistantResponseDto>.Ok(fallback, "Response served via SmartStock assistant (Gemini offline fallback).");
            }

            var responseString = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            var parsedJson = JsonNode.Parse(responseString);

            string? generatedText = null;
            var partsNode = parsedJson?["candidates"]?[0]?["content"]?["parts"]?.AsArray();
            if (partsNode != null && partsNode.Count > 0)
            {
                var texts = partsNode.Select(p => p?["text"]?.ToString()).Where(t => !string.IsNullOrWhiteSpace(t));
                generatedText = string.Join("\n", texts);
            }
            else
            {
                generatedText = parsedJson?["candidates"]?[0]?["content"]?[0]?["text"]?.ToString();
            }

            if (string.IsNullOrWhiteSpace(generatedText))
            {
                var fallback = GenerateDeterministicFallback(question, businessName, products.Count, totalStockValue, totalSalesToday, totalSalesMonth, lowStockProducts, outOfStockProducts, topSellers);
                return ApiResponse<AIAssistantResponseDto>.Ok(fallback, "Response served via SmartStock assistant.");
            }

            var aiResponse = new AIAssistantResponseDto
            {
                Answer = generatedText.Trim(),
                SuggestedFollowUps = GenerateFollowUpSuggestions(question),
                IsAiGenerated = true,
                Timestamp = DateTime.UtcNow
            };

            return ApiResponse<AIAssistantResponseDto>.Ok(aiResponse, "AI assistant response retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception communicating with Gemini API. Returning deterministic fallback.");
            var fallback = GenerateDeterministicFallback(question, businessName, products.Count, totalStockValue, totalSalesToday, totalSalesMonth, lowStockProducts, outOfStockProducts, topSellers);
            return ApiResponse<AIAssistantResponseDto>.Ok(fallback, "Response served via SmartStock assistant fallback.");
        }
    }

    private static AIAssistantResponseDto GenerateDeterministicFallback(
        string question,
        string storeName,
        int productCount,
        decimal totalStockValue,
        decimal salesToday,
        decimal salesMonth,
        List<Product> lowStock,
        List<Product> outOfStock,
        dynamic topSellers)
    {
        var lower = question.ToLowerInvariant();
        string answer;

        if (lower.Contains("reorder") || lower.Contains("buy") || lower.Contains("order"))
        {
            if (outOfStock.Count > 0 || lowStock.Count > 0)
            {
                var outNames = outOfStock.Select(p => p.Name).ToList();
                var lowNames = lowStock.Where(p => p.CurrentStock > 0).Select(p => $"{p.Name} ({p.CurrentStock} left)").ToList();
                
                var sb = new StringBuilder();
                sb.AppendLine($"Here are the urgent reorder recommendations for **{storeName}**:");
                if (outNames.Count > 0)
                {
                    sb.AppendLine($"- **Out of Stock**: {string.Join(", ", outNames)}.");
                }
                if (lowNames.Count > 0)
                {
                    sb.AppendLine($"- **Low Stock**: {string.Join(", ", lowNames)}.");
                }
                sb.AppendLine("Please check your Purchase Recommendations tab to create purchase orders for these items.");
                answer = sb.ToString();
            }
            else
            {
                answer = $"All items in **{storeName}** currently have healthy inventory levels above their minimum safety thresholds. No urgent reorders required today.";
            }
        }
        else if (lower.Contains("value") || lower.Contains("worth") || lower.Contains("inventory"))
        {
            answer = $"Your total inventory valuation is **Rs. {totalStockValue:N2}** across **{productCount}** active products. You have {lowStock.Count} items requiring stock replenishment.";
        }
        else if (lower.Contains("fast") || lower.Contains("top") || lower.Contains("selling") || lower.Contains("popular"))
        {
            var topList = new List<string>();
            foreach (var item in topSellers)
            {
                topList.Add($"{item.Name} ({item.Qty} units, Rs. {item.Revenue:N0})");
            }
            answer = topList.Count > 0
                ? $"Your top selling products are: {string.Join(", ", topList)}."
                : "No sales history recorded yet to determine top sellers.";
        }
        else if (lower.Contains("today") || lower.Contains("sale") || lower.Contains("month"))
        {
            answer = $"**{storeName}** Sales Overview:\n- **Sales Today**: Rs. {salesToday:N2}\n- **Sales This Month**: Rs. {salesMonth:N2}\nKeep tracking daily cash flow and top items.";
        }
        else if (lower.Contains("low") || lower.Contains("out") || lower.Contains("alert"))
        {
            answer = $"You currently have **{outOfStockProductsCount(outOfStock)}** out-of-stock items and **{lowStock.Count}** low-stock items. Immediate reordering is recommended.";
        }
        else
        {
            answer = $"Hello! At **{storeName}**, you have {productCount} active products valued at Rs. {totalStockValue:N2}. " +
                     $"Sales this month stand at Rs. {salesMonth:N2}. You have {lowStock.Count} low-stock alerts. How can I help optimize your shop's operations today?";
        }

        return new AIAssistantResponseDto
        {
            Answer = answer,
            SuggestedFollowUps = GenerateFollowUpSuggestions(question),
            IsAiGenerated = false,
            Timestamp = DateTime.UtcNow
        };

        static int outOfStockProductsCount(List<Product> o) => o.Count;
    }

    private static List<string> GenerateFollowUpSuggestions(string question)
    {
        var lower = question.ToLowerInvariant();
        if (lower.Contains("reorder") || lower.Contains("stock"))
        {
            return new List<string>
            {
                "Which products have low stock?",
                "What is my total inventory value?",
                "Which products are selling fastest?"
            };
        }

        return new List<string>
        {
            "Which products should I reorder?",
            "How much did I sell this month?",
            "What is my total inventory value?"
        };
    }
}
