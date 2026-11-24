using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Interfaces;

namespace SmartStock.API.Controllers;

[Authorize]
[Route("api/v1/recurring-orders")]
public class RecurringOrdersController : BaseApiController
{
    private readonly IRecurringOrderService _recurringOrderService;
    private readonly IRecurringOrderSchedulerService _schedulerService;

    public RecurringOrdersController(
        IRecurringOrderService recurringOrderService,
        IRecurringOrderSchedulerService schedulerService)
    {
        _recurringOrderService = recurringOrderService;
        _schedulerService = schedulerService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<RecurringOrderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] RecurringOrderFilterParams filter, CancellationToken cancellationToken)
    {
        var result = await _recurringOrderService.GetPagedAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RecurringOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _recurringOrderService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("generate-draft/{customerId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RecurringOrderDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> GenerateDraft(Guid customerId, [FromQuery] DateTime? scheduledDate, CancellationToken cancellationToken)
    {
        var date = scheduledDate ?? DateTime.UtcNow;
        var result = await _recurringOrderService.GenerateDraftOrderForCustomerAsync(customerId, date, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("{id:guid}/finalize-to-sale")]
    [ProducesResponseType(typeof(ApiResponse<SaleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> FinalizeToSale(Guid id, CancellationToken cancellationToken)
    {
        var result = await _recurringOrderService.FinalizeOrderToSaleAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(Guid id, [FromQuery] string? reason, CancellationToken cancellationToken)
    {
        var result = await _recurringOrderService.CancelOrderAsync(id, reason, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/resolve-review")]
    [ProducesResponseType(typeof(ApiResponse<RecurringOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResolveReview(Guid id, [FromBody] ManualOrderReviewResolutionDto dto, CancellationToken cancellationToken)
    {
        var result = await _recurringOrderService.ResolveManualReviewAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpGet("dashboard-metrics")]
    [ProducesResponseType(typeof(ApiResponse<RecurringDashboardMetricsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardMetrics(CancellationToken cancellationToken)
    {
        var result = await _recurringOrderService.GetDashboardMetricsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("trigger-scheduler-cycle")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TriggerSchedulerCycle(CancellationToken cancellationToken)
    {
        var count = await _schedulerService.ProcessUpcomingRecurringOrdersCycleAsync(cancellationToken);
        return Ok(ApiResponse<int>.Ok(count, $"Processed {count} orders in scheduler cycle."));
    }
}
