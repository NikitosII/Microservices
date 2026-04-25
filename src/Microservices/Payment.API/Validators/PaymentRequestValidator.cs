using FluentValidation;
using Payment.API.Models;

namespace Payment.API.Validators
{
    public class PaymentRequestValidator : AbstractValidator<PaymentRequest>
    {
        public PaymentRequestValidator()
        {
            RuleFor(x => x.OrderId)
                .NotEmpty().WithMessage("OrderId is required.");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Payment amount must be > 0.");

            RuleFor(x => x.Currency)
                .NotEmpty()
                .Length(3)
                .Matches("^[A-Z]{3}$").WithMessage("Currency must be a ISO code, 3 uppercase letters (e.g. USD).");

            RuleFor(x => x.CustomerEmail)
                .EmailAddress().When(x => !string.IsNullOrEmpty(x.CustomerEmail))
                .WithMessage("Customer email must be a valid email address.");

            When(x => x.Method == PaymentMethod.CreditCard, () =>
            {
                RuleFor(x => x.CardDetails)
                    .NotNull()
                    .WithMessage("Card details are required for credit card payments.");

                RuleFor(x => x.CardDetails!.Number)
                    .NotEmpty()
                    .CreditCard()
                    .WithMessage("Card number is invalid.");

                RuleFor(x => x.CardDetails!.ExpiryMonth)
                    .NotEmpty()
                    .Matches("^(0[1-9]|1[0-2])$")
                    .WithMessage("Expiry month must be in MM format (01-12).");

                RuleFor(x => x.CardDetails!.ExpiryYear)
                    .NotEmpty()
                    .Matches(@"^\d{4}$")
                    .WithMessage("Expiry year must be a 4-digit year.");

                RuleFor(x => x.CardDetails!.Cvc)
                    .NotEmpty()
                    .Matches(@"^\d{3,4}$")
                    .WithMessage("CVC must be 3 or 4 digits.");

                RuleFor(x => x.CardDetails!.CardholderName)
                    .NotEmpty().WithMessage("Cardholder name is required.")
                    .MaximumLength(100);
            });
        }
    }
}
