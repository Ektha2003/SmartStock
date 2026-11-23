using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Interfaces;

namespace SmartStock.API.Controllers;

[Authorize]
[Route("api/v1/[controller]")]
public class DashboardController : BaseApiController
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Retrieve business dashboard KPIs, counts, inventory value, and recent activities.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<DashboardSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetSummaryAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve daily or monthly revenue and volume chart trends.
    /// </summary>
    [HttpGet("sales-chart")]
    [ProducesResponseType(typeof(ApiResponse<SalesChartResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSalesChart(
        [FromQuery] string interval = "daily",
        [FromQuery] int count = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetSalesChartAsync(interval, count, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve top-selling products by revenue and units.
    /// </summary>
    [HttpGet("top-products")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TopProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopProducts([FromQuery] int top = 5, CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetTopProductsAsync(top, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve inventory valuation grouped by product category.
    /// </summary>
    [HttpGet("stock-value")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StockValueCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockValue(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetStockValueByCategoryAsync(cancellationToken);
        return Ok(result);
    }
}
