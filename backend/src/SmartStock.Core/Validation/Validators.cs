using FluentValidation;
using SmartStock.Core.DTOs;

namespace SmartStock.Core.Validation;

public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Full name is required.").MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.").EmailAddress().WithMessage("A valid email is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.").MinimumLength(6).WithMessage("Password must be at least 6 characters.");
        RuleFor(x => x.BusinessName).NotEmpty().WithMessage("Business name is required.").MaximumLength(200);
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.").EmailAddress().WithMessage("A valid email is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}

public class CreateCategoryValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Category name is required.").MaximumLength(100);
    }
}

public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Category name is required.").MaximumLength(100);
    }
}

public class CreateProductValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Product name is required.").MaximumLength(200);
        RuleFor(x => x.SKU).NotEmpty().WithMessage("SKU is required.").MaximumLength(100);
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Category is required.");
        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0).WithMessage("Purchase price cannot be negative.");
        RuleFor(x => x.SellingPrice).GreaterThanOrEqualTo(0).WithMessage("Selling price cannot be negative.");
        RuleFor(x => x.CurrentStock).GreaterThanOrEqualTo(0).WithMessage("Initial stock cannot be negative.");
        RuleFor(x => x.MinimumStock).GreaterThanOrEqualTo(0).WithMessage("Minimum stock threshold cannot be negative.");
    }
}

public class UpdateProductValidator : AbstractValidator<UpdateProductDto>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Product name is required.").MaximumLength(200);
        RuleFor(x => x.SKU).NotEmpty().WithMessage("SKU is required.").MaximumLength(100);
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Category is required.");
        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0).WithMessage("Purchase price cannot be negative.");
        RuleFor(x => x.SellingPrice).GreaterThanOrEqualTo(0).WithMessage("Selling price cannot be negative.");
        RuleFor(x => x.MinimumStock).GreaterThanOrEqualTo(0).WithMessage("Minimum stock threshold cannot be negative.");
    }
}

public class CreateSupplierValidator : AbstractValidator<CreateSupplierDto>
{
    public CreateSupplierValidator()
    {
        RuleFor(x => x.SupplierName).NotEmpty().WithMessage("Supplier name is required.").MaximumLength(200);
    }
}

public class CreateSaleValidator : AbstractValidator<CreateSaleDto>
{
    public CreateSaleValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("Sale must contain at least one product item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty().WithMessage("Product ID is required.");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative.");
        });
    }
}

public class CreatePurchaseValidator : AbstractValidator<CreatePurchaseDto>
{
    public CreatePurchaseValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("Purchase must contain at least one item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty().WithMessage("Product ID is required.");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
            item.RuleFor(i => i.UnitCost).GreaterThanOrEqualTo(0).WithMessage("Unit cost cannot be negative.");
        });
    }
}

public class StockAdjustmentValidator : AbstractValidator<StockAdjustmentDto>
{
    public StockAdjustmentValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Product ID is required.");
        RuleFor(x => x.NewQuantity).GreaterThanOrEqualTo(0).WithMessage("Adjusted quantity cannot be negative.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Reason for stock adjustment is mandatory for auditing.").MaximumLength(500);
    }
}
