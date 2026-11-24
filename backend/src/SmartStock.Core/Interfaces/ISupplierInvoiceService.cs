using SmartStock.Core.Common;
using SmartStock.Core.DTOs;

namespace SmartStock.Core.Interfaces;

public interface ISupplierInvoiceService
{
    Task<ApiResponse<SupplierInvoiceDto>> UploadAndProcessInvoiceAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        Guid? supplierId = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResult<SupplierInvoiceDto>>> GetInvoicesAsync(
        SupplierInvoiceFilterParams filter,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<SupplierInvoiceDto>> GetInvoiceByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<SupplierInvoiceDto>> RematchProductsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<SupplierInvoiceItemDto>> UpdateInvoiceItemAsync(
        Guid invoiceId,
        Guid itemId,
        UpdateSupplierInvoiceItemDto dto,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<SupplierInvoiceItemDto>> ConfirmProductMatchAsync(
        Guid invoiceId,
        ConfirmProductMatchDto dto,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<SupplierInvoiceItemDto>> CreateProductFromInvoiceItemAsync(
        Guid invoiceId,
        CreateProductFromInvoiceDto dto,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<SupplierInvoiceDto>> ApproveAndSyncInventoryAsync(
        Guid invoiceId,
        ApproveSupplierInvoiceDto dto,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<SupplierInvoiceDto>> RejectInvoiceAsync(
        Guid invoiceId,
        string? reason,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<SupplierInvoiceDashboardMetricsDto>> GetDashboardMetricsAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<InvoiceComparisonDto>> CompareInvoiceWithPreviousAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<AIInvoiceAssistantResponseDto>> QueryAssistantAsync(
        AIInvoiceAssistantQueryDto dto,
        CancellationToken cancellationToken = default);
}
