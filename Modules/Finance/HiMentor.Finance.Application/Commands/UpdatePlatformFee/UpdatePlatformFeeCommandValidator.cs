using FluentValidation;

namespace HiMentor.Finance.Application.Commands.UpdatePlatformFee;

public sealed class UpdatePlatformFeeCommandValidator : AbstractValidator<UpdatePlatformFeeCommand>
{
    public UpdatePlatformFeeCommandValidator()
    {
        RuleFor(x => x.Percentage).InclusiveBetween(0, 100)
            .WithMessage("O percentual da plataforma deve estar entre 0 e 100.");
        RuleFor(x => x.AdminUserId).NotEmpty();
    }
}
