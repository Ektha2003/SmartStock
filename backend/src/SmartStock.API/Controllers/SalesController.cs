using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Interfaces;

namespace SmartStock.API.Controllers;

[Authorize]
[Route("api/v1/[controller]")]
public class SalesController : BaseApiController
{
    private readonly ISalesService _salesService;

    public SalesController(ISalesService salesService)
    {
        _salesService = salesService;
    }

    /// <summary>
    /// Record a new multi-item sale, deduct inventory, and generate an audited transaction.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SaleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleDto dto, CancellationToken cancellationToken)
    {
        var result = await _salesService.CreateSaleAsync(dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Retrieve paginated sale history with optional date filtering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SaleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSales(
        [FromQuery] PaginationParams pagination,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var result = await _salesService.GetSalesAsync(pagination, fromDate, toDate, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve invoice details of a specific sale.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SaleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSaleById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _salesService.GetSaleByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve total sales summary (today, this month, all time).
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<SalesSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSalesSummary(CancellationToken cancellationToken)
    {
        var result = await _salesService.GetSalesSummaryAsync(cancellationToken);
        return Ok(result);
    }
}
