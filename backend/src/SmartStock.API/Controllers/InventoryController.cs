using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Enums;
using SmartStock.Core.Interfaces;

namespace SmartStock.API.Controllers;

[Authorize]
[Route("api/v1/[controller]")]
public class InventoryController : BaseApiController
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    /// <summary>
    /// Retrieve full inventory metrics, valuation, total quantity, and stock health counts.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<InventorySummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetSummaryAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve paginated ledger of stock movements and transactions.
    /// </summary>
    [HttpGet("transactions")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<StockTransactionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] PaginationParams pagination,
        [FromQuery] Guid? productId,
        [FromQuery] TransactionType? type,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetTransactionsAsync(pagination, productId, type, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve the complete historical stock transaction timeline for a single product.
    /// </summary>
    [HttpGet("product/{productId:guid}/history")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StockTransactionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductHistory(Guid productId, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetProductHistoryAsync(productId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Perform a manual stock adjustment with mandatory audit explanation.
    /// </summary>
    [HttpPost("adjustment")]
    [ProducesResponseType(typeof(ApiResponse<StockTransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdjustStock([FromBody] StockAdjustmentDto dto, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.AdjustStockAsync(dto, cancellationToken);
        return Ok(result);
    }
}
