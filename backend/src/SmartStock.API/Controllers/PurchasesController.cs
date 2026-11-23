using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Interfaces;

namespace SmartStock.API.Controllers;

[Authorize]
[Route("api/v1/[controller]")]
public class PurchasesController : BaseApiController
{
    private readonly IPurchaseService _purchaseService;

    public PurchasesController(IPurchaseService purchaseService)
    {
        _purchaseService = purchaseService;
    }

    /// <summary>
    /// Record a purchase order from a supplier and replenish stock automatically.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PurchaseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePurchase([FromBody] CreatePurchaseDto dto, CancellationToken cancellationToken)
    {
        var result = await _purchaseService.CreatePurchaseAsync(dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Retrieve paginated purchase invoices.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PurchaseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPurchases([FromQuery] PaginationParams pagination, CancellationToken cancellationToken)
    {
        var result = await _purchaseService.GetPurchasesAsync(pagination, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get detailed purchase invoice by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPurchaseById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _purchaseService.GetPurchaseByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get purchase expenditures summary.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<PurchasesSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPurchasesSummary(CancellationToken cancellationToken)
    {
        var result = await _purchaseService.GetPurchasesSummaryAsync(cancellationToken);
        return Ok(result);
    }
}
