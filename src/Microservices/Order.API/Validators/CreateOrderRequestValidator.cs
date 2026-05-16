using FluentValidation;
using Order.API.Models;

namespace Order.API.Validators
{
    public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
    {
        private static readonly string[] AllowedPaymentMethods = { "CreditCard", "PayPal", "CashOnDelivery" };

        public CreateOrderRequestValidator()
        {
            RuleFor(x => x.PaymentMethod)
                .NotEmpty()
                .Must(m => AllowedPaymentMethods.Contains(m))
                .WithMessage($"Payment method must be one of: {string.Join(", ", AllowedPaymentMethods)}.");

            RuleFor(x => x.CouponCode)
                .MaximumLength(50).When(x => !string.IsNullOrEmpty(x.CouponCode));

            RuleFor(x => x.ShippingAddress).NotNull().WithMessage("Shipping address is required.");

            RuleFor(x => x.ShippingAddress.FullName)
                .NotEmpty().WithMessage("Full name is required.")
                .MaximumLength(100);

            RuleFor(x => x.ShippingAddress.Street)
                .NotEmpty().WithMessage("Street address is required.")
                .MaximumLength(200);

            RuleFor(x => x.ShippingAddress.City)
                .NotEmpty().WithMessage("City is required.")
                .MaximumLength(100);

            RuleFor(x => x.ShippingAddress.Country)
                .NotEmpty().WithMessage("Country is required.")
                .MaximumLength(100);

            RuleFor(x => x.ShippingAddress.ZipCode)
                .NotEmpty().WithMessage("Zip code is required.")
                .MaximumLength(20);

            RuleFor(x => x.ShippingAddress.Phone)
                .NotEmpty().WithMessage("Phone number is required.")
                .MaximumLength(20);
        }
    }
}
