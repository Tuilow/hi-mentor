using MediatR;

namespace Tuilow.Finance.Application.Queries.GetMyFinancialOnboardingStatus;

public sealed record GetMyFinancialOnboardingStatusQuery(Guid CreatorId) : IRequest<CreatorFinancialOnboardingStatusResponse>;

/// <summary>Um passo da jornada amigável (item 11 do briefing) — State é "done" | "current" | "pending" | "blocked", nunca um código interno da Asaas.</summary>
public sealed record OnboardingStepResponse(string Key, string Title, string State);

public sealed record OnboardingDocumentResponse(string Id, string Title, string? Description, string Status, string? OnboardingUrl);

/// <summary>
/// Dados já preenchidos pelo criador (nunca inclui API Key/Wallet ID/qualquer coisa que só exista
/// depois da subconta criada) -- devolvido só enquanto o formulário do passo 1 ainda pode ser
/// reenviado (NotStarted/CollectingData/Rejected, ver CreatorAsaasSubaccount.StartCollectingData),
/// para o criador não precisar redigitar tudo depois de uma rejeição.
/// </summary>
public sealed record PreviousOnboardingDataResponse(
    string LegalName, string CpfCnpj, DateOnly? BirthDate, string? CompanyType,
    string Email, string MobilePhone, string? Phone, decimal IncomeValue,
    string Address, string AddressNumber, string? AddressComplement, string Province, string PostalCode
);

public sealed record CreatorFinancialOnboardingStatusResponse(
    string Status, // enum bruto (NotStarted/CollectingData/...) -- uso interno/depuração, o frontend usa Steps
    bool CanSell,
    string? FriendlyMessage,
    IReadOnlyList<OnboardingStepResponse> Steps,
    IReadOnlyList<OnboardingDocumentResponse> Documents,
    PreviousOnboardingDataResponse? PreviousData
);
