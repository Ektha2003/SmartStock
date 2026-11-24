using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Interfaces;

namespace SmartStock.API.Controllers;

[Authorize]
[Route("api/v1/customers/{customerId:guid}/recurring-products")]
public class CustomerRecurringProductsController : BaseApiController
{
    private readonly ICustomerRecurringProductService _recurringProductService;

    public CustomerRecurringProductsController(ICustomerRecurringProductService recurringProductService)
    {
        _recurringProductService = recurringProductService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CustomerRecurringProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(Guid customerId, CancellationToken cancellationToken)
    {
        var result = await _recurringProductService.GetProductsAsync(customerId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CustomerRecurringProductDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddProduct(Guid customerId, [FromBody] CreateCustomerRecurringProductDto dto, CancellationToken cancellationToken)
    {
        var result = await _recurringProductService.AddProductAsync(customerId, dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{recurringProductId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerRecurringProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProduct(Guid customerId, Guid recurringProductId, [FromBody] UpdateCustomerRecurringProductDto dto, CancellationToken cancellationToken)
    {
        var result = await _recurringProductService.UpdateProductAsync(customerId, recurringProductId, dto, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{recurringProductId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveProduct(Guid customerId, Guid recurringProductId, CancellationToken cancellationToken)
    {
        var result = await _recurringProductService.RemoveProductAsync(customerId, recurringProductId, cancellationToken);
        return Ok(result);
    }
}
