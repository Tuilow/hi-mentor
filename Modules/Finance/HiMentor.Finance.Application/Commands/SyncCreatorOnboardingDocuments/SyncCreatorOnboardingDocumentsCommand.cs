using MediatR;

namespace HiMentor.Finance.Application.Commands.SyncCreatorOnboardingDocuments;

/// <summary>
/// Consulta GET /v3/myAccount/documents na Asaas e reconcilia a lista local de documentos —
/// chamado pelo frontend ao entrar na etapa "Documentação" e sempre que o criador clica em
/// "Atualizar". Idempotente por natureza (é uma releitura, não uma escrita) — pode ser chamado
/// quantas vezes forem necessárias sem efeito colateral.
/// </summary>
public sealed record SyncCreatorOnboardingDocumentsCommand(Guid CreatorId) : IRequest<SyncCreatorOnboardingDocumentsResult>;

public sealed record SyncCreatorOnboardingDocumentsResult(bool Success, string Status, string? ErrorMessage);
