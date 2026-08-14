using HiMentor.SharedKernel.Domain.Common;
using HiMentor.Finance.Domain.Enums;

namespace HiMentor.Finance.Domain.Entities;

/// <summary>
/// Um documento pendente/enviado/aprovado da subconta do criador, sincronizado sempre a partir de
/// GET /v3/myAccount/documents (nunca uma lista fixa no código — ver item 5/6 do briefing de
/// onboarding financeiro). Filha de <see cref="CreatorAsaasSubaccount"/>.
///
/// Quando <see cref="OnboardingUrl"/> está preenchido, a documentação oficial da Asaas exige que
/// o envio aconteça naquela página hospedada pela Asaas (não é possível enviar via API para esses
/// documentos) — o frontend deve abrir esse link externamente. Quando é nulo, o envio pode ser
/// feito diretamente pela HiMentor via <see cref="Interfaces.IAsaasSubaccountClient.UploadDocumentAsync"/>.
/// </summary>
public sealed class CreatorAsaasOnboardingDocument : Entity
{
    public Guid CreatorAsaasSubaccountId { get; private set; }

    /// <summary>Id do documento na Asaas (usado em POST /v3/myAccount/documents/{id}).</summary>
    public string AsaasDocumentId { get; private set; } = string.Empty;

    /// <summary>Tipo retornado pela Asaas (ex.: IDENTIFICATION, IDENTIFICATION_SELFIE) — texto livre, nunca mapeado para um enum fechado.</summary>
    public string Type { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public OnboardingDocumentStatus Status { get; private set; } = OnboardingDocumentStatus.Pending;

    /// <summary>Link hospedado pela Asaas para envio deste documento específico — nulo quando o envio pode ser feito via API.</summary>
    public string? OnboardingUrl { get; private set; }

    private CreatorAsaasOnboardingDocument() { }

    public static CreatorAsaasOnboardingDocument Create(
        Guid creatorAsaasSubaccountId, string asaasDocumentId, string type, string title,
        string? description, OnboardingDocumentStatus status, string? onboardingUrl) => new()
    {
        CreatorAsaasSubaccountId = creatorAsaasSubaccountId,
        AsaasDocumentId = asaasDocumentId,
        Type = type,
        Title = title,
        Description = description,
        Status = status,
        OnboardingUrl = onboardingUrl
    };

    /// <summary>Atualiza os campos mutáveis a partir de uma nova leitura da Asaas (mesmo AsaasDocumentId).</summary>
    public void SyncFrom(string title, string? description, OnboardingDocumentStatus status, string? onboardingUrl)
    {
        Title = title;
        Description = description;
        Status = status;
        OnboardingUrl = onboardingUrl;
        Touch();
    }
}
