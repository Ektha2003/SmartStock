using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Enums;

namespace SmartStock.Core.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? BusinessId { get; }
    string? Email { get; }
    UserRole? Role { get; }
    bool IsAuthenticated { get; }
    Guid GetRequiredBusinessId();
}

public interface IAuthService
{
    Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}

public interface IBusinessService
{
    Task<ApiResponse<BusinessDto>> GetCurrentBusinessAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<BusinessDto>> UpdateBusinessAsync(UpdateBusinessDto dto, CancellationToken cancellationToken = default);
}

public interface ICategoryService
{
    Task<ApiResponse<IReadOnlyList<CategoryDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<CategoryDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<CategoryDto>> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IProductService
{
    Task<ApiResponse<PagedResult<ProductDto>>> GetPagedAsync(ProductFilterParams filterParams, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductDto>> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductDto>> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<ProductDto>>> GetLowStockProductsAsync(CancellationToken cancellationToken = default);
}

public interface ISupplierService
{
    Task<ApiResponse<PagedResult<SupplierDto>>> GetPagedAsync(PaginationParams pagination, CancellationToken cancellationToken = default);
    Task<ApiResponse<SupplierDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<SupplierDto>> CreateAsync(CreateSupplierDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<SupplierDto>> UpdateAsync(Guid id, UpdateSupplierDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<SupplierDetailExtendedDto>> GetDetailExtendedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<SupplierSuppliedProductDto>>> GetSuppliedProductsAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<SupplierInvoiceHistoryDto>>> GetSupplierInvoicesAsync(Guid supplierId, PaginationParams pagination, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<SupplierPurchaseHistoryDto>>> GetSupplierPurchasesAsync(Guid supplierId, PaginationParams pagination, CancellationToken cancellationToken = default);
}

public interface ISalesService
{
    Task<ApiResponse<SaleDto>> CreateSaleAsync(CreateSaleDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<SaleDto>>> GetSalesAsync(PaginationParams pagination, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<SaleDto>> GetSaleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<SalesSummaryDto>> GetSalesSummaryAsync(CancellationToken cancellationToken = default);
}

public interface IPurchaseService
{
    Task<ApiResponse<PurchaseDto>> CreatePurchaseAsync(CreatePurchaseDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<PurchaseDto>>> GetPurchasesAsync(PaginationParams pagination, CancellationToken cancellationToken = default);
    Task<ApiResponse<PurchaseDto>> GetPurchaseByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<PurchasesSummaryDto>> GetPurchasesSummaryAsync(CancellationToken cancellationToken = default);
}

public interface IInventoryService
{
    Task<ApiResponse<InventorySummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<StockTransactionDto>>> GetTransactionsAsync(PaginationParams pagination, Guid? productId = null, TransactionType? type = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<StockTransactionDto>>> GetProductHistoryAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ApiResponse<StockTransactionDto>> AdjustStockAsync(StockAdjustmentDto dto, CancellationToken cancellationToken = default);
}

public interface IDashboardService
{
    Task<ApiResponse<DashboardSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<SalesChartResponseDto>> GetSalesChartAsync(string interval = "daily", int count = 30, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<TopProductDto>>> GetTopProductsAsync(int top = 5, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<StockValueCategoryDto>>> GetStockValueByCategoryAsync(CancellationToken cancellationToken = default);
}

public interface INotificationService
{
    Task<ApiResponse<IReadOnlyList<NotificationDto>>> GetNotificationsAsync(bool unreadOnly = false, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> MarkAllAsReadAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> CheckAndGenerateStockAlertsAsync(CancellationToken cancellationToken = default);
}

public interface IForecastingService
{
    Task<ApiResponse<IReadOnlyList<ForecastDto>>> GetForecastsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<ForecastDto>> GetProductForecastAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<PurchaseRecommendationDto>>> GetPurchaseRecommendationsAsync(CancellationToken cancellationToken = default);
}

public interface IAIAssistantService
{
    Task<ApiResponse<AIAssistantResponseDto>> AskAssistantAsync(AIAssistantRequestDto request, CancellationToken cancellationToken = default);
}

public interface IInvoiceScannerService
{
    Task<ApiResponse<InvoiceExtractResponseDto>> ExtractInvoiceAsync(Stream imageStream, string fileName, string contentType, CancellationToken cancellationToken = default);
}
