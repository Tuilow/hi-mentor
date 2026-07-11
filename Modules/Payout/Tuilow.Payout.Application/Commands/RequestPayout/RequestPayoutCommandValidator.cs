using FluentValidation;

namespace Tuilow.Payout.Application.Commands.RequestPayout;

public sealed class RequestPayoutCommandValidator : AbstractValidator<RequestPayoutCommand>
{
    public RequestPayoutCommandValidator()
    {
        RuleFor(x => x.CreatorId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).When(x => x.Amount is not null)
            .WithMessage("O valor do saque deve ser maior que zero.");
    }
}
