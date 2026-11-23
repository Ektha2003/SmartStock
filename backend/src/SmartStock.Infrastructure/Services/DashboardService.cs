using Microsoft.EntityFrameworkCore;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Enums;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Data;

namespace SmartStock.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DashboardService(ApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<DashboardSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var today = DateTime.UtcNow.Date;
        var startOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalProducts = await _context.Products.CountAsync(p => p.BusinessId == businessId && p.IsActive, cancellationToken);
        var totalCategories = await _context.Categories.CountAsync(c => c.BusinessId == businessId, cancellationToken);

        var totalInventoryValue = await _context.Products
            .Where(p => p.BusinessId == businessId && p.IsActive)
            .SumAsync(p => (decimal?)(p.CurrentStock * p.PurchasePrice), cancellationToken) ?? 0;

        var totalSalesToday = await _context.Sales
            .Where(s => s.BusinessId == businessId && s.SaleDate >= today)
            .SumAsync(s => (decimal?)s.TotalAmount, cancellationToken) ?? 0;

        var totalSalesThisMonth = await _context.Sales
            .Where(s => s.BusinessId == businessId && s.SaleDate >= startOfMonth)
            .SumAsync(s => (decimal?)s.TotalAmount, cancellationToken) ?? 0;

        var lowStockCount = await _context.Products
            .CountAsync(p => p.BusinessId == businessId && p.IsActive && p.CurrentStock <= p.MinimumStock && p.CurrentStock > 0, cancellationToken);

        var outOfStockCount = await _context.Products
            .CountAsync(p => p.BusinessId == businessId && p.IsActive && p.CurrentStock <= 0, cancellationToken);

        var recentSales = await _context.Sales
            .Include(s => s.Items)
            .ThenInclude(i => i.Product)
            .Where(s => s.BusinessId == businessId)
            .OrderByDescending(s => s.SaleDate)
            .Take(5)
            .Select(s => new SaleDto
            {
                Id = s.Id,
                BusinessId = s.BusinessId,
                InvoiceNumber = s.InvoiceNumber,
                TotalAmount = s.TotalAmount,
                SaleDate = s.SaleDate,
                Notes = s.Notes,
                CreatedAt = s.CreatedAt,
                Items = s.Items.Select(i => new SaleItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    SKU = i.Product.SKU,
                    Unit = i.Product.Unit,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    SubTotal = i.SubTotal
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var recentPurchases = await _context.Purchases
            .Include(p => p.Supplier)
            .Include(p => p.Items)
            .ThenInclude(i => i.Product)
            .Where(p => p.BusinessId == businessId)
            .OrderByDescending(p => p.PurchaseDate)
            .Take(5)
            .Select(p => new PurchaseDto
            {
                Id = p.Id,
                BusinessId = p.BusinessId,
                SupplierId = p.SupplierId,
                SupplierName = p.Supplier != null ? p.Supplier.SupplierName : null,
                InvoiceNumber = p.InvoiceNumber,
                TotalAmount = p.TotalAmount,
                PurchaseDate = p.PurchaseDate,
                Notes = p.Notes,
                CreatedAt = p.CreatedAt,
                Items = p.Items.Select(i => new PurchaseItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    SKU = i.Product.SKU,
                    Quantity = i.Quantity,
                    UnitCost = i.UnitCost,
                    SubTotal = i.SubTotal
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var summary = new DashboardSummaryDto
        {
            TotalProducts = totalProducts,
            TotalCategories = totalCategories,
            TotalInventoryValue = totalInventoryValue,
            TotalSalesToday = totalSalesToday,
            TotalSalesThisMonth = totalSalesThisMonth,
            LowStockProductCount = lowStockCount,
            OutOfStockProductCount = outOfStockCount,
            RecentSales = recentSales,
            RecentPurchases = recentPurchases
        };

        return ApiResponse<DashboardSummaryDto>.Ok(summary, "Dashboard summary retrieved successfully.");
    }

    public async Task<ApiResponse<SalesChartResponseDto>> GetSalesChartAsync(string interval = "daily", int count = 30, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        count = Math.Clamp(count, 1, 90);

        var response = new SalesChartResponseDto
        {
            Interval = interval.ToLower() == "monthly" ? "monthly" : "daily"
        };

        if (response.Interval == "daily")
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-count + 1);

            var sales = await _context.Sales
                .Where(s => s.BusinessId == businessId && s.SaleDate >= startDate)
                .Select(s => new { s.SaleDate, s.TotalAmount })
                .ToListAsync(cancellationToken);

            var grouped = sales
                .GroupBy(s => s.SaleDate.ToString("yyyy-MM-dd"))
                .ToDictionary(g => g.Key, g => new { Total = g.Sum(x => x.TotalAmount), Count = g.Count() });

            for (int i = 0; i < count; i++)
            {
                var dateKey = startDate.AddDays(i).ToString("yyyy-MM-dd");
                if (grouped.TryGetValue(dateKey, out var val))
                {
                    response.DataPoints.Add(new SalesChartPointDto
                    {
                        Period = dateKey,
                        TotalSales = val.Total,
                        OrderCount = val.Count
                    });
                }
                else
                {
                    response.DataPoints.Add(new SalesChartPointDto
                    {
                        Period = dateKey,
                        TotalSales = 0,
                        OrderCount = 0
                    });
                }
            }
        }
        else
        {
            // Monthly for last 12 months
            var monthsCount = Math.Clamp(count, 1, 12);
            var startMonth = DateTime.UtcNow.Date.AddMonths(-monthsCount + 1);
            var firstOfMonth = new DateTime(startMonth.Year, startMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var sales = await _context.Sales
                .Where(s => s.BusinessId == businessId && s.SaleDate >= firstOfMonth)
                .Select(s => new { s.SaleDate, s.TotalAmount })
                .ToListAsync(cancellationToken);

            var grouped = sales
                .GroupBy(s => s.SaleDate.ToString("yyyy-MM"))
                .ToDictionary(g => g.Key, g => new { Total = g.Sum(x => x.TotalAmount), Count = g.Count() });

            for (int i = 0; i < monthsCount; i++)
            {
                var monthKey = firstOfMonth.AddMonths(i).ToString("yyyy-MM");
                if (grouped.TryGetValue(monthKey, out var val))
                {
                    response.DataPoints.Add(new SalesChartPointDto
                    {
                        Period = monthKey,
                        TotalSales = val.Total,
                        OrderCount = val.Count
                    });
                }
                else
                {
                    response.DataPoints.Add(new SalesChartPointDto
                    {
                        Period = monthKey,
                        TotalSales = 0,
                        OrderCount = 0
                    });
                }
            }
        }

        return ApiResponse<SalesChartResponseDto>.Ok(response, "Sales chart data retrieved successfully.");
    }

    public async Task<ApiResponse<IReadOnlyList<TopProductDto>>> GetTopProductsAsync(int top = 5, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        top = Math.Clamp(top, 1, 20);

        var topProducts = await _context.SaleItems
            .Include(si => si.Product)
            .ThenInclude(p => p.Category)
            .Where(si => si.Product.BusinessId == businessId)
            .GroupBy(si => new { si.ProductId, si.Product.Name, si.Product.SKU, CategoryName = si.Product.Category.Name, si.Product.CurrentStock })
            .Select(g => new TopProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                SKU = g.Key.SKU,
                CategoryName = g.Key.CategoryName,
                TotalQuantitySold = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.SubTotal),
                CurrentStock = g.Key.CurrentStock
            })
            .OrderByDescending(x => x.TotalRevenue)
            .Take(top)
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyList<TopProductDto>>.Ok(topProducts, "Top selling products retrieved successfully.");
    }

    public async Task<ApiResponse<IReadOnlyList<StockValueCategoryDto>>> GetStockValueByCategoryAsync(CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var stockValues = await _context.Categories
            .Where(c => c.BusinessId == businessId)
            .Select(c => new StockValueCategoryDto
            {
                CategoryId = c.Id,
                CategoryName = c.Name,
                ProductCount = c.Products.Count(p => p.IsActive),
                StockValue = c.Products.Where(p => p.IsActive).Sum(p => p.CurrentStock * p.PurchasePrice)
            })
            .OrderByDescending(x => x.StockValue)
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyList<StockValueCategoryDto>>.Ok(stockValues, "Inventory valuation by category retrieved successfully.");
    }
}
