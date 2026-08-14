using FluentValidation;

namespace HiMentor.Finance.Application.Commands.ConnectCreatorAsaasAccount;

public sealed class ConnectCreatorAsaasAccountCommandValidator : AbstractValidator<ConnectCreatorAsaasAccountCommand>
{
    public ConnectCreatorAsaasAccountCommandValidator()
    {
        RuleFor(x => x.CreatorId).NotEmpty();
        RuleFor(x => x.ApiKey).NotEmpty().WithMessage("Informe a API Key da sua conta Asaas.");
        RuleFor(x => x.CpfCnpj).MaximumLength(20);
        RuleFor(x => x.LegalName).MaximumLength(200);
    }
}
