using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Interfaces;

namespace SmartStock.API.Controllers;

[Authorize]
[Route("api/v1/[controller]")]
public class SuppliersController : BaseApiController
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    /// <summary>
    /// Retrieve paginated list of registered store suppliers.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SupplierDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] PaginationParams pagination, CancellationToken cancellationToken)
    {
        var result = await _supplierService.GetPagedAsync(pagination, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get supplier profile by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SupplierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _supplierService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Register a new supplier.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SupplierDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSupplierDto dto, CancellationToken cancellationToken)
    {
        var result = await _supplierService.CreateAsync(dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Update supplier details.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SupplierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSupplierDto dto, CancellationToken cancellationToken)
    {
        var result = await _supplierService.UpdateAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Deactivate (soft delete) a supplier.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _supplierService.DeleteAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get supplier profile with recent invoices, purchases, and supplied products.
    /// </summary>
    [HttpGet("{id:guid}/detail")]
    [ProducesResponseType(typeof(ApiResponse<SupplierDetailExtendedDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDetailExtended(Guid id, CancellationToken cancellationToken)
    {
        var result = await _supplierService.GetDetailExtendedAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get products supplied by this supplier.
    /// </summary>
    [HttpGet("{id:guid}/products")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SupplierSuppliedProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuppliedProducts(Guid id, CancellationToken cancellationToken)
    {
        var result = await _supplierService.GetSuppliedProductsAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get invoices for this supplier.
    /// </summary>
    [HttpGet("{id:guid}/invoices")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SupplierInvoiceHistoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSupplierInvoices(Guid id, [FromQuery] PaginationParams pagination, CancellationToken cancellationToken)
    {
        var result = await _supplierService.GetSupplierInvoicesAsync(id, pagination, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get purchase transactions for this supplier.
    /// </summary>
    [HttpGet("{id:guid}/purchases")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SupplierPurchaseHistoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSupplierPurchases(Guid id, [FromQuery] PaginationParams pagination, CancellationToken cancellationToken)
    {
        var result = await _supplierService.GetSupplierPurchasesAsync(id, pagination, cancellationToken);
        return Ok(result);
    }
}
