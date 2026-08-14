using FluentValidation;

namespace HiMentor.IdentidadeAcesso.Application.Commands.ResetPassword;

/// <summary>Mesma regra de força de senha usada em RegisterUserCommandValidator.</summary>
public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token é obrigatório.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .MinimumLength(8).WithMessage("A senha deve ter no mínimo 8 caracteres.")
            .Matches("[A-Z]").WithMessage("A senha deve conter ao menos uma letra maiúscula.")
            .Matches("[0-9]").WithMessage("A senha deve conter ao menos um número.");
    }
}
