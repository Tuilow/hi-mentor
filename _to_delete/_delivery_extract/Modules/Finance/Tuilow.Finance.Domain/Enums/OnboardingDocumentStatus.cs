namespace Tuilow.Finance.Domain.Enums;

/// <summary>
/// Espelha o campo "status" retornado por GET /v3/myAccount/documents da Asaas — nunca uma lista
/// fixa/hardcoded, sempre sincronizado a partir da resposta real (ver
/// IAsaasSubaccountClient.GetPendingDocumentsAsync).
/// </summary>
public enum OnboardingDocumentStatus
{
    Pending = 0,
    AwaitingApproval = 1,
    Approved = 2,
    Rejected = 3
}
