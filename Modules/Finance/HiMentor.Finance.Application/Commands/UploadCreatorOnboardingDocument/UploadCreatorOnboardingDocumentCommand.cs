using MediatR;

namespace HiMentor.Finance.Application.Commands.UploadCreatorOnboardingDocument;

/// <summary>
/// Envia um documento diretamente pela HiMentor (proxy de stream para a Asaas, nunca persistido em
/// disco/banco/log da HiMentor) — só válido para documentos SEM OnboardingUrl. Documentos com
/// OnboardingUrl exigem obrigatoriamente o link externo hospedado pela Asaas (limitação da
/// própria plataforma, ver CreatorAsaasOnboardingDocument).
/// </summary>
public sealed record UploadCreatorOnboardingDocumentCommand(
    Guid CreatorId, string AsaasDocumentId, Stream FileStream, string FileName, string ContentType
) : IRequest<UploadCreatorOnboardingDocumentResult>;

public sealed record UploadCreatorOnboardingDocumentResult(bool Success, string? ErrorMessage);
