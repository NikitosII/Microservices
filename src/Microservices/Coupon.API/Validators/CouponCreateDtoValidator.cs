using Coupon.API.Models;
using FluentValidation;

namespace Coupon.API.Validators
{
    public class CouponCreateDtoValidator : AbstractValidator<CouponCreateDto>
    {
        public CouponCreateDtoValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Coupon code is required.")
                .MaximumLength(50)
                .Matches("^[A-Z0-9_-]+$")
                .WithMessage("Coupon code must be uppercase letters, numbers, hyphens, or underscores.");

            RuleFor(x => x.DiscountAmount)
                .GreaterThan(0)
                .WithMessage("Discount amount must be > 0.");

            RuleFor(x => x.DiscountType)
                .Must(t => t == "Fixed" || t == "Percentage")
                .WithMessage("Discount type must be 'Fixed' or 'Percentage'.");

            RuleFor(x => x.DiscountAmount)
                .LessThanOrEqualTo(100)
                .When(x => x.DiscountType == "Percentage")
                .WithMessage("Percentage discount cannot exceed 100.");

            RuleFor(x => x.ValidFrom)
                .NotEmpty()
                .WithMessage("ValidFrom date is required.");

            RuleFor(x => x.ValidUntil)
                .NotEmpty()
                .GreaterThan(x => x.ValidFrom)
                .WithMessage("ValidUntil date is required and must be after ValidFrom.");

            RuleFor(x => x.MaxUsageCount)
                .GreaterThan(0)
                .WithMessage("Max usage count must be at least 1.");

            RuleFor(x => x.MinimumAmount)
                .GreaterThanOrEqualTo(0).When(x => x.MinimumAmount.HasValue)
                .WithMessage("Minimum amount cannot be negative.");

            RuleFor(x => x.MaximumDiscount)
                .GreaterThan(0).When(x => x.MaximumDiscount.HasValue)
                .WithMessage("Maximum discount must be greater than 0 (when specified).");
        }
    }
}
