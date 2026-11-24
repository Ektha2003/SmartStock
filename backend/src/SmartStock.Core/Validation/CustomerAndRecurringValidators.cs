using FluentValidation;
using SmartStock.Core.DTOs;

namespace SmartStock.Core.Validation;

public class CreateCustomerValidator : AbstractValidator<CreateCustomerDto>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Customer name is required.").MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email address is required.").EmailAddress().WithMessage("A valid email address is required.");
        RuleFor(x => x.Phone).NotEmpty().WithMessage("Mobile number is required.").MaximumLength(30);
        RuleFor(x => x.DeliveryAddress).NotEmpty().WithMessage("Delivery address is required.").MaximumLength(500);
        RuleFor(x => x.BillingAddress).MaximumLength(500);
    }
}

public class UpdateCustomerValidator : AbstractValidator<UpdateCustomerDto>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Customer name is required.").MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email address is required.").EmailAddress().WithMessage("A valid email address is required.");
        RuleFor(x => x.Phone).NotEmpty().WithMessage("Mobile number is required.").MaximumLength(30);
        RuleFor(x => x.DeliveryAddress).NotEmpty().WithMessage("Delivery address is required.").MaximumLength(500);
        RuleFor(x => x.BillingAddress).MaximumLength(500);
    }
}

public class CreateCustomerRecurringProductValidator : AbstractValidator<CreateCustomerRecurringProductDto>
{
    public CreateCustomerRecurringProductValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Product selection is required.");
        RuleFor(x => x.DefaultQuantity).GreaterThan(0).WithMessage("Default quantity must be greater than zero.");
        RuleFor(x => x.MinQuantity).GreaterThan(0).When(x => x.MinQuantity.HasValue).WithMessage("Minimum quantity must be greater than zero.");
        RuleFor(x => x.MaxQuantity).GreaterThan(0).When(x => x.MaxQuantity.HasValue).WithMessage("Maximum quantity must be greater than zero.");
        RuleFor(x => x.MaxQuantity).GreaterThanOrEqualTo(x => x.MinQuantity!.Value)
            .When(x => x.MinQuantity.HasValue && x.MaxQuantity.HasValue)
            .WithMessage("Maximum quantity cannot be less than minimum quantity.");
        RuleFor(x => x.PriceOverride).GreaterThanOrEqualTo(0).When(x => x.PriceOverride.HasValue)
            .WithMessage("Price override cannot be negative.");
    }
}

public class UpdateCustomerRecurringProductValidator : AbstractValidator<UpdateCustomerRecurringProductDto>
{
    public UpdateCustomerRecurringProductValidator()
    {
        RuleFor(x => x.DefaultQuantity).GreaterThan(0).WithMessage("Default quantity must be greater than zero.");
        RuleFor(x => x.MinQuantity).GreaterThan(0).When(x => x.MinQuantity.HasValue).WithMessage("Minimum quantity must be greater than zero.");
        RuleFor(x => x.MaxQuantity).GreaterThan(0).When(x => x.MaxQuantity.HasValue).WithMessage("Maximum quantity must be greater than zero.");
        RuleFor(x => x.MaxQuantity).GreaterThanOrEqualTo(x => x.MinQuantity!.Value)
            .When(x => x.MinQuantity.HasValue && x.MaxQuantity.HasValue)
            .WithMessage("Maximum quantity cannot be less than minimum quantity.");
        RuleFor(x => x.PriceOverride).GreaterThanOrEqualTo(0).When(x => x.PriceOverride.HasValue)
            .WithMessage("Price override cannot be negative.");
    }
}

public class UpdateCustomerRecurringSettingValidator : AbstractValidator<UpdateCustomerRecurringSettingDto>
{
    public UpdateCustomerRecurringSettingValidator()
    {
        RuleFor(x => x.IntervalDays).GreaterThan(0).WithMessage("Interval days must be at least 1 day.");
        RuleFor(x => x.ReminderBeforeDeliveryDays).GreaterThanOrEqualTo(0).WithMessage("Reminder days cannot be negative.");
    }
}

public class CustomerPortalSubmitValidator : AbstractValidator<CustomerPortalSubmitDto>
{
    public CustomerPortalSubmitValidator()
    {
        RuleFor(x => x.Action).NotEmpty().WithMessage("Action is required.");
    }
}
