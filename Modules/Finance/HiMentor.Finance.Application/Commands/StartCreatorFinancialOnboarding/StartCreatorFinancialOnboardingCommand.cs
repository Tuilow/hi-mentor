using MediatR;

namespace HiMentor.Finance.Application.Commands.StartCreatorFinancialOnboarding;

/// <summary>
/// Passo 1 da jornada ("Seus dados") — o criador informa os dados exigidos pela Asaas para criar
/// sua subconta (POST /v3/accounts), e o handler dispara a criação de forma idempotente: se o
/// criador já tem uma subconta (AsaasAccountId preenchido), a chamada à Asaas nunca é repetida —
/// só os dados cadastrais locais são atualizados (ver item 15 do briefing, "não pode criar duas
/// subcontas Asaas para o mesmo criador").
/// </summary>
public sealed record StartCreatorFinancialOnboardingCommand(
    Guid CreatorId,
    string LegalName,
    string CpfCnpj,
    DateOnly? BirthDate,
    string? CompanyType,
    string Email,
    string MobilePhone,
    string? Phone,
    decimal IncomeValue,
    string Address,
    string AddressNumber,
    string? AddressComplement,
    string Province,
    string PostalCode
) : IRequest<StartCreatorFinancialOnboardingResult>;

public sealed record StartCreatorFinancialOnboardingResult(bool Success, string Status, string? ErrorMessage);
