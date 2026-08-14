using HiMentor.Finance.Domain.Entities;
using HiMentor.Finance.Domain.Enums;
using HiMentor.Finance.Domain.Interfaces;
using MediatR;

namespace HiMentor.Finance.Application.Queries.GetMyFinancialOnboardingStatus;

/// <summary>
/// Traduz o estado interno (CreatorOnboardingStatus + documentos) para a jornada de 5 passos
/// amigável do item 11 do briefing -- nenhuma menção a "subconta"/"API Key"/"Wallet ID" chega ao
/// frontend a partir daqui.
/// </summary>
public sealed class GetMyFinancialOnboardingStatusQueryHandler(ICreatorAsaasSubaccountRepository repository)
    : IRequestHandler<GetMyFinancialOnboardingStatusQuery, CreatorFinancialOnboardingStatusResponse>
{
    public async Task<CreatorFinancialOnboardingStatusResponse> Handle(GetMyFinancialOnboardingStatusQuery request, CancellationToken ct)
    {
        var subaccount = await repository.GetByCreatorIdAsync(request.CreatorId, ct);

        if (subaccount is null)
        {
            return new CreatorFinancialOnboardingStatusResponse(
                CreatorOnboardingStatus.NotStarted.ToString(), false, null, BuildSteps(CreatorOnboardingStatus.NotStarted), [], null);
        }

        var documents = subaccount.Documents
            .Select(d => new OnboardingDocumentResponse(d.AsaasDocumentId, d.Title, d.Description, d.Status.ToString(), d.OnboardingUrl))
            .ToList();

        var friendlyMessage = subaccount.Status switch
        {
            CreatorOnboardingStatus.Rejected => subaccount.RejectionReason,
            CreatorOnboardingStatus.Blocked => "Sua conta financeira está temporariamente bloqueada. Fale com o suporte.",
            _ => null
        };

        // Só faz sentido devolver os dados já digitados enquanto o passo 1 ainda pode ser
        // reenviado (ver StartCollectingData) -- uma vez com AsaasAccountId preenchido, os dados
        // cadastrais só mudam direto com a Asaas, então não há formulário para prencher aqui.
        PreviousOnboardingDataResponse? previousData = subaccount.AsaasAccountId is null
            ? new PreviousOnboardingDataResponse(
                subaccount.LegalName, subaccount.CpfCnpj, subaccount.BirthDate, subaccount.CompanyType,
                subaccount.Email, subaccount.MobilePhone, subaccount.Phone, subaccount.IncomeValue,
                subaccount.Address, subaccount.AddressNumber, subaccount.AddressComplement, subaccount.Province, subaccount.PostalCode)
            : null;

        return new CreatorFinancialOnboardingStatusResponse(
            subaccount.Status.ToString(), subaccount.CanSell, friendlyMessage, BuildSteps(subaccount.Status), documents, previousData);
    }

    private static IReadOnlyList<OnboardingStepResponse> BuildSteps(CreatorOnboardingStatus status)
    {
        string DataState() => status == CreatorOnboardingStatus.NotStarted ? "current"
            : status == CreatorOnboardingStatus.CollectingData ? "current" : "done";

        string DocsState() => status switch
        {
            CreatorOnboardingStatus.NotStarted or CreatorOnboardingStatus.CollectingData or CreatorOnboardingStatus.AccountCreationPending => "pending",
            CreatorOnboardingStatus.AccountCreated or CreatorOnboardingStatus.DocumentsPending => "current",
            _ => "done"
        };

        string ReviewState() => status switch
        {
            CreatorOnboardingStatus.UnderReview => "current",
            CreatorOnboardingStatus.Approved => "done",
            CreatorOnboardingStatus.Rejected => "blocked",
            _ => "pending"
        };

        string ApprovalState() => status switch
        {
            CreatorOnboardingStatus.Approved => "done",
            CreatorOnboardingStatus.Rejected => "blocked",
            CreatorOnboardingStatus.Blocked => "blocked",
            _ => "pending"
        };

        string SellState() => status == CreatorOnboardingStatus.Approved ? "done" : "pending";

        return
        [
            new("dados", "Seus dados", DataState()),
            new("documentos", "Documentação", DocsState()),
            new("analise", "Análise", ReviewState()),
            new("conta", "Conta financeira", ApprovalState()),
            new("vender", "Pronto para vender", SellState())
        ];
    }
}
