using FluentValidation;

namespace Tuilow.Finance.Application.Commands.StartCreatorFinancialOnboarding;

/// <summary>
/// Só valida presença/tamanho — as regras de negócio da Asaas (CPF x CNPJ, obrigatoriedade de
/// birthDate/companyType) são responsabilidade do próprio agregado/handler, não deste validator
/// de borda (mesmo padrão de ConnectCreatorAsaasAccountCommandValidator).
/// </summary>
public sealed class StartCreatorFinancialOnboardingCommandValidator : AbstractValidator<StartCreatorFinancialOnboardingCommand>
{
    public StartCreatorFinancialOnboardingCommandValidator()
    {
        RuleFor(x => x.CreatorId).NotEmpty();
        RuleFor(x => x.LegalName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CpfCnpj).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.MobilePhone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.IncomeValue).GreaterThan(0).WithMessage("Informe sua renda/faturamento mensal aproximado.");
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.AddressNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Province).NotEmpty().MaximumLength(150);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(12);

        var cpfDigits = new Func<string, int>(v => v.Count(char.IsDigit));

        // CPF (11 dígitos) exige data de nascimento; CNPJ (14 dígitos) exige companyType — mesma
        // distinção documentada pela Asaas (ver CreatorAsaasSubaccount).
        RuleFor(x => x.BirthDate)
            .NotNull()
            .When(x => cpfDigits(x.CpfCnpj) == 11)
            .WithMessage("Data de nascimento é obrigatória para pessoa física.");

        RuleFor(x => x.CompanyType)
            .NotEmpty()
            .When(x => cpfDigits(x.CpfCnpj) == 14)
            .WithMessage("Tipo de empresa é obrigatório para pessoa jurídica.");
    }
}
