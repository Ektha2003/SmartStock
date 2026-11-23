using Microsoft.EntityFrameworkCore;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Entities;
using SmartStock.Core.Enums;
using SmartStock.Core.Exceptions;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Data;

namespace SmartStock.Infrastructure.Forecasting;

public class ForecastingService : IForecastingService
{
    private const int VelocityWindowDays = 7;
    private const int TargetCoverageDays = 10;

    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ForecastingService(ApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<IReadOnlyList<ForecastDto>>> GetForecastsAsync(CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var cutoff = DateTime.UtcNow.AddDays(-VelocityWindowDays);

        var products = await _context.Products
            .Include(p => p.Category)
            .Where(p => p.BusinessId == businessId && p.IsActive)
            .ToListAsync(cancellationToken);

        // Fetch sales in the last 7 days
        var recentSaleItems = await _context.SaleItems
            .Include(si => si.Sale)
            .Where(si => si.Sale.BusinessId == businessId && si.Sale.SaleDate >= cutoff)
            .ToListAsync(cancellationToken);

        var salesByProduct = recentSaleItems
            .GroupBy(si => si.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        var forecasts = products.Select(p =>
        {
            var unitsSoldLast7Days = salesByProduct.TryGetValue(p.Id, out var sold) ? sold : 0;
            return CalculateProductForecast(p, unitsSoldLast7Days);
        })
        .OrderBy(f => f.RiskLevel) // Critical and High first
        .ThenBy(f => f.EstimatedDaysRemaining)
        .ToList();

        return ApiResponse<IReadOnlyList<ForecastDto>>.Ok(forecasts, "Demand forecasts computed successfully.");
    }

    public async Task<ApiResponse<ForecastDto>> GetProductForecastAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var cutoff = DateTime.UtcNow.AddDays(-VelocityWindowDays);

        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId && p.BusinessId == businessId, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", productId);
        }

        var unitsSoldLast7Days = await _context.SaleItems
            .Include(si => si.Sale)
            .Where(si => si.ProductId == productId && si.Sale.BusinessId == businessId && si.Sale.SaleDate >= cutoff)
            .SumAsync(si => (decimal?)si.Quantity, cancellationToken) ?? 0;

        var forecast = CalculateProductForecast(product, unitsSoldLast7Days);
        return ApiResponse<ForecastDto>.Ok(forecast, "Product forecast computed successfully.");
    }

    public async Task<ApiResponse<IReadOnlyList<PurchaseRecommendationDto>>> GetPurchaseRecommendationsAsync(CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var cutoff = DateTime.UtcNow.AddDays(-VelocityWindowDays);

        var products = await _context.Products
            .Include(p => p.Category)
            .Where(p => p.BusinessId == businessId && p.IsActive)
            .ToListAsync(cancellationToken);

        var recentSaleItems = await _context.SaleItems
            .Include(si => si.Sale)
            .Where(si => si.Sale.BusinessId == businessId && si.Sale.SaleDate >= cutoff)
            .ToListAsync(cancellationToken);

        var salesByProduct = recentSaleItems
            .GroupBy(si => si.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        var recommendations = new List<PurchaseRecommendationDto>();

        foreach (var p in products)
        {
            var unitsSoldLast7Days = salesByProduct.TryGetValue(p.Id, out var sold) ? sold : 0;
            var forecast = CalculateProductForecast(p, unitsSoldLast7Days);

            // Recommend purchases for items requiring order (SuggestedOrderQuantity > 0 or CurrentStock <= MinimumStock)
            if (forecast.SuggestedOrderQuantity > 0 || p.CurrentStock <= p.MinimumStock)
            {
                var suggestedQty = forecast.SuggestedOrderQuantity;
                if (suggestedQty <= 0 && p.CurrentStock <= p.MinimumStock)
                {
                    // Fallback reorder buffer to bring up to at least double the minimum stock
                    suggestedQty = Math.Max(1, (p.MinimumStock * 2) - p.CurrentStock);
                }

                var roundedQty = Math.Ceiling(suggestedQty);
                var estCost = Math.Round(roundedQty * p.PurchasePrice, 2);

                string reason;
                if (p.CurrentStock <= 0)
                {
                    reason = "Product is out of stock. Immediate replenishment required.";
                }
                else if (forecast.EstimatedDaysRemaining <= 2)
                {
                    reason = $"Critically low inventory. At current sales velocity ({forecast.AverageDailySales:F1} {p.Unit}/day), stock will run out in {forecast.EstimatedDaysRemaining:F1} days.";
                }
                else if (p.CurrentStock <= p.MinimumStock)
                {
                    reason = $"Current stock ({p.CurrentStock} {p.Unit}) has reached or fallen below minimum safety threshold ({p.MinimumStock} {p.Unit}).";
                }
                else
                {
                    reason = $"Predicted stock exhaustion in {forecast.EstimatedDaysRemaining:F1} days based on 7-day velocity. Order recommended for {TargetCoverageDays}-day buffer.";
                }

                recommendations.Add(new PurchaseRecommendationDto
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    SKU = p.SKU,
                    CategoryName = p.Category.Name,
                    CurrentStock = p.CurrentStock,
                    AverageDailySales = forecast.AverageDailySales,
                    EstimatedDaysRemaining = forecast.EstimatedDaysRemaining,
                    SuggestedOrderQuantity = roundedQty,
                    UnitPurchasePrice = p.PurchasePrice,
                    EstimatedPurchaseCost = estCost,
                    RiskLevel = forecast.RiskLevel,
                    Reason = reason
                });
            }
        }

        var orderedRecommendations = recommendations
            .OrderBy(r => r.RiskLevel)
            .ThenBy(r => r.EstimatedDaysRemaining)
            .ToList();

        return ApiResponse<IReadOnlyList<PurchaseRecommendationDto>>.Ok(orderedRecommendations, "Purchase recommendations generated successfully.");
    }

    private static ForecastDto CalculateProductForecast(Product product, decimal unitsSoldLast7Days)
    {
        var avgDailySales = Math.Round(unitsSoldLast7Days / (decimal)VelocityWindowDays, 2);
        var targetStock = Math.Round(avgDailySales * TargetCoverageDays, 2);
        var suggestedOrderQty = Math.Max(0, targetStock - product.CurrentStock);

        double daysRemaining;
        StockRiskLevel risk;
        string explanation;
        bool hasSufficientData = true;

        if (product.CurrentStock <= 0)
        {
            daysRemaining = 0;
            risk = StockRiskLevel.Critical;
            explanation = "Item is out of stock. Customers cannot purchase this product.";
        }
        else if (avgDailySales <= 0)
        {
            // Zero sales during last 7 days
            daysRemaining = 999; // Represents stationary / slow mover
            risk = product.CurrentStock <= product.MinimumStock ? StockRiskLevel.High : StockRiskLevel.Healthy;
            hasSufficientData = false;
            explanation = product.CurrentStock <= product.MinimumStock
                ? "No sales in the last 7 days, but current stock is below minimum safety threshold."
                : "No sales in the last 7 days. Stock is stationary.";
        }
        else
        {
            daysRemaining = Math.Round((double)(product.CurrentStock / avgDailySales), 1);

            if (daysRemaining <= 2)
            {
                risk = StockRiskLevel.Critical;
                explanation = $"Critical: Stock is projected to deplete within {daysRemaining:F1} days at current velocity ({avgDailySales:F1} {product.Unit}/day).";
            }
            else if (daysRemaining <= 5)
            {
                risk = StockRiskLevel.High;
                explanation = $"High Risk: Stock will likely last only {daysRemaining:F1} days. Place reorder soon.";
            }
            else if (daysRemaining <= 10)
            {
                risk = StockRiskLevel.Medium;
                explanation = $"Moderate Risk: Estimated {daysRemaining:F1} days of supply remaining.";
            }
            else
            {
                risk = StockRiskLevel.Healthy;
                explanation = $"Healthy: Inventory is well-stocked for approximately {daysRemaining:F1} days.";
            }
        }

        return new ForecastDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            SKU = product.SKU,
            CategoryName = product.Category?.Name ?? string.Empty,
            CurrentStock = product.CurrentStock,
            TotalUnitsSoldLast7Days = unitsSoldLast7Days,
            AverageDailySales = avgDailySales,
            EstimatedDaysRemaining = daysRemaining,
            TargetStock = targetStock,
            SuggestedOrderQuantity = suggestedOrderQty,
            RiskLevel = risk,
            HasSufficientData = hasSufficientData,
            Explanation = explanation
        };
    }
}
