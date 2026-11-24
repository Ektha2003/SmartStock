using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Interfaces;

namespace SmartStock.API.Controllers;

[Authorize]
[Route("api/v1/[controller]")]
public class CustomersController : BaseApiController
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CustomerDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] CustomerFilterParams filter, CancellationToken cancellationToken)
    {
        var result = await _customerService.GetPagedAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _customerService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto, CancellationToken cancellationToken)
    {
        var result = await _customerService.CreateAsync(dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerDto dto, CancellationToken cancellationToken)
    {
        var result = await _customerService.UpdateAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/toggle-status")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleStatus(Guid id, CancellationToken cancellationToken)
    {
        var result = await _customerService.ToggleStatusAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _customerService.DeleteAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/orders")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<RecurringOrderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrderHistory(Guid id, [FromQuery] PaginationParams pagination, CancellationToken cancellationToken)
    {
        var result = await _customerService.GetOrderHistoryAsync(id, pagination, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/upcoming-orders")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RecurringOrderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUpcomingOrders(Guid id, CancellationToken cancellationToken)
    {
        var result = await _customerService.GetUpcomingOrdersAsync(id, cancellationToken);
        return Ok(result);
    }
}
