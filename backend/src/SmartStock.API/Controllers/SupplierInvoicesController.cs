using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Interfaces;

namespace SmartStock.API.Controllers;

[Authorize]
[Route("api/v1/supplier-invoices")]
public class SupplierInvoicesController : BaseApiController
{
    private readonly ISupplierInvoiceService _supplierInvoiceService;

    public SupplierInvoicesController(ISupplierInvoiceService supplierInvoiceService)
    {
        _supplierInvoiceService = supplierInvoiceService;
    }

    /// <summary>
    /// Upload and process a supplier invoice (PDF, JPG, PNG). Extracts OCR data and matches products.
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<SupplierInvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadInvoice(
        IFormFile file,
        [FromForm] Guid? supplierId = null,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<object>.Fail("A valid invoice document (PDF, PNG, JPG) is required."));
        }

        using var stream = file.OpenReadStream();
        var result = await _supplierInvoiceService.UploadAndProcessInvoiceAsync(
            stream,
            file.FileName,
            file.ContentType,
            supplierId,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get paginated list of supplier invoices with filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SupplierInvoiceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] SupplierInvoiceFilterParams filter,
        CancellationToken cancellationToken = default)
    {
        var result = await _supplierInvoiceService.GetInvoicesAsync(filter, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get single supplier invoice by ID with line items and match details.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SupplierInvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _supplierInvoiceService.GetInvoiceByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Trigger re-evaluation of product matching against catalog.
    /// </summary>
    [HttpPost("{id:guid}/rematch")]
    [ProducesResponseType(typeof(ApiResponse<SupplierInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RematchProducts(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _supplierInvoiceService.RematchProductsAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update invoice item details (conversion factor, pack size, manual product mapping, price flag).
    /// </summary>
    [HttpPut("{id:guid}/items/{itemId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SupplierInvoiceItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateItem(
        Guid id,
        Guid itemId,
        [FromBody] UpdateSupplierInvoiceItemDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _supplierInvoiceService.UpdateInvoiceItemAsync(id, itemId, dto, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Confirm product match for an invoice line item.
    /// </summary>
    [HttpPost("{id:guid}/confirm-match")]
    [ProducesResponseType(typeof(ApiResponse<SupplierInvoiceItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmMatch(
        Guid id,
        [FromBody] ConfirmProductMatchDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _supplierInvoiceService.ConfirmProductMatchAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new inventory product directly from an unmatched invoice item.
    /// </summary>
    [HttpPost("{id:guid}/add-product")]
    [ProducesResponseType(typeof(ApiResponse<SupplierInvoiceItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddProduct(
        Guid id,
        [FromBody] CreateProductFromInvoiceDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _supplierInvoiceService.CreateProductFromInvoiceItemAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Final approval of invoice: atomically updates stock, creates Purchase record and stock transactions.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(ApiResponse<SupplierInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveInvoice(
        Guid id,
        [FromBody] ApproveSupplierInvoiceDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _supplierInvoiceService.ApproveAndSyncInventoryAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Reject an invoice.
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(ApiResponse<SupplierInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectInvoice(
        Guid id,
        [FromQuery] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _supplierInvoiceService.RejectInvoiceAsync(id, reason, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get dashboard metrics for supplier invoices (pending review, approved, price hikes, monthly spend).
    /// </summary>
    [HttpGet("dashboard-metrics")]
    [ProducesResponseType(typeof(ApiResponse<SupplierInvoiceDashboardMetricsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardMetrics(CancellationToken cancellationToken = default)
    {
        var result = await _supplierInvoiceService.GetDashboardMetricsAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Compare invoice with the previous invoice from the same supplier to analyze price & quantity variance.
    /// </summary>
    [HttpGet("{id:guid}/compare")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceComparisonDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CompareInvoice(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _supplierInvoiceService.CompareInvoiceWithPreviousAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Query the AI Assistant about invoices, price hikes, duplicate detection, or supplier history.
    /// </summary>
    [HttpPost("assistant-query")]
    [ProducesResponseType(typeof(ApiResponse<AIInvoiceAssistantResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> QueryAssistant(
        [FromBody] AIInvoiceAssistantQueryDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _supplierInvoiceService.QueryAssistantAsync(dto, cancellationToken);
        return Ok(result);
    }
}
