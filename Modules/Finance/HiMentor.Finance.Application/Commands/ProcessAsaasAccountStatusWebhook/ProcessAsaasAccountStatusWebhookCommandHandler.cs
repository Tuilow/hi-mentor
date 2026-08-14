using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Finance.Domain.Entities;
using HiMentor.Finance.Domain.Enums;
using HiMentor.Finance.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiMentor.Finance.Application.Commands.ProcessAsaasAccountStatusWebhook;

/// <summary>
/// Processa eventos ACCOUNT_STATUS_* (ver AsaasAccountWebhookController). Idempotente por
/// EventId (a Asaas entrega "at-least-once" — o mesmo evento pode chegar mais de uma vez, ver
/// ProcessedAsaasAccountEvent) e resiliente a evento fora de ordem: um REJECTED que chega depois
/// de um APPROVED (ex.: reentrega atrasada de um evento antigo) não deveria reverter uma
/// aprovação já confirmada por um evento mais recente -- por isso o único evento que muda o
/// estado para Approved é justamente GENERAL_APPROVAL_APPROVED, e o handler não tenta reconstruir
/// uma ordem cronológica a partir de dateCreated (a Asaas não garante isso) -- ele aplica cada
/// evento como um fato pontual sobre a categoria correspondente (documentação, comercial, conta
/// bancária, aprovação geral), deixando GENERAL_APPROVAL como a fonte de verdade de CanSell.
///
/// Consequência prática dessa garantia (achado durante os testes, ver
/// HiMentor.Finance.Tests.Application.ProcessAsaasAccountStatusWebhookCommandHandlerTests): um
/// REJECTED de CATEGORIA (documento/comercial/conta bancária) só é aplicado enquanto a subconta
/// ainda não estiver Approved -- uma vez aprovada pelo evento GENERAL_APPROVAL_APPROVED, só um
/// novo GENERAL_APPROVAL_REJECTED (o evento "guarda-chuva" que a própria Asaas envia se o status
/// geral de fato mudar) pode derrubar CanSell de novo. Mesma disciplina que MarkUnderReview() já
/// aplicava (ver CreatorAsaasSubaccount) -- sem essa guarda, um evento de categoria reentregue com
/// atraso depois da aprovação derrubava CanSell silenciosamente, o que descumpria o item 17 do
/// briefing ("resistente a webhooks fora de ordem").
/// </summary>
public sealed class ProcessAsaasAccountStatusWebhookCommandHandler(
    ICreatorAsaasSubaccountRepository subaccountRepository,
    IProcessedAsaasAccountEventRepository processedEventRepository,
    IUnitOfWork uow,
    ILogger<ProcessAsaasAccountStatusWebhookCommandHandler> logger
) : IRequestHandler<ProcessAsaasAccountStatusWebhookCommand>
{
    public async Task Handle(ProcessAsaasAccountStatusWebhookCommand request, CancellationToken ct)
    {
        var payload = request.Payload;

        if (await processedEventRepository.ExistsAsync(payload.Id, ct))
        {
            logger.LogInformation("Evento de status de conta {EventId} já processado — reenvio ignorado (idempotência).", payload.Id);
            return;
        }

        var subaccount = await subaccountRepository.GetByAsaasAccountIdAsync(payload.Account.Id, ct);
        if (subaccount is null)
        {
            logger.LogWarning("Evento de status de conta para AsaasAccountId {AccountId} sem CreatorAsaasSubaccount correspondente — ignorado.", payload.Account.Id);
            await MarkProcessedAsync(payload, ct);
            return;
        }

        switch (payload.Event)
        {
            case "ACCOUNT_STATUS_GENERAL_APPROVAL_APPROVED":
                subaccount.MarkApproved();
                logger.LogInformation("Onboarding financeiro aprovado para o criador {CreatorId}.", subaccount.CreatorId);
                break;

            case "ACCOUNT_STATUS_GENERAL_APPROVAL_REJECTED":
                // Evento "guarda-chuva": sempre aplicado, mesmo que a subconta já estivesse
                // Approved -- é a própria Asaas revisando a aprovação geral concedida antes.
                subaccount.MarkRejected("Encontramos uma pendência na sua documentação ou nos seus dados cadastrais. Reveja as informações enviadas.");
                logger.LogWarning("Onboarding financeiro rejeitado para o criador {CreatorId} (evento {Event}).", subaccount.CreatorId, payload.Event);
                break;

            case "ACCOUNT_STATUS_DOCUMENT_REJECTED":
            case "ACCOUNT_STATUS_COMMERCIAL_INFO_REJECTED":
            case "ACCOUNT_STATUS_BANK_ACCOUNT_INFO_REJECTED":
                // Sinal de CATEGORIA (não o veredito geral) -- se a subconta já foi aprovada
                // (GENERAL_APPROVAL_APPROVED), um evento de categoria reentregue com atraso não
                // deve derrubar CanSell sozinho (ver nota de classe sobre resiliência a webhooks
                // fora de ordem); só um novo GENERAL_APPROVAL_REJECTED faz isso.
                if (subaccount.Status == CreatorOnboardingStatus.Approved)
                {
                    logger.LogInformation(
                        "Evento de categoria {Event} recebido para criador {CreatorId} já aprovado -- ignorado sem regressão (aguardando eventual GENERAL_APPROVAL_REJECTED).",
                        payload.Event, subaccount.CreatorId);
                    break;
                }

                // Mensagem propositalmente genérica -- nunca expõe o código bruto da Asaas ao
                // criador (ver item 11 do briefing: "não mostrar códigos internos da Asaas"). O
                // detalhe de qual categoria rejeitou fica só no log, para suporte investigar.
                subaccount.MarkRejected("Encontramos uma pendência na sua documentação ou nos seus dados cadastrais. Reveja as informações enviadas.");
                logger.LogWarning("Onboarding financeiro rejeitado para o criador {CreatorId} (evento {Event}).", subaccount.CreatorId, payload.Event);
                break;

            case "ACCOUNT_STATUS_GENERAL_APPROVAL_AWAITING_APPROVAL":
            case "ACCOUNT_STATUS_DOCUMENT_AWAITING_APPROVAL":
            case "ACCOUNT_STATUS_COMMERCIAL_INFO_AWAITING_APPROVAL":
            case "ACCOUNT_STATUS_BANK_ACCOUNT_INFO_AWAITING_APPROVAL":
                subaccount.MarkUnderReview();
                break;

            default:
                // Eventos de PENDING/EXPIRING_SOON/EXPIRED e demais categorias: registrados só
                // para idempotência/auditoria, sem transição de estado própria -- o criador é
                // orientado pela consulta de documentos pendentes (GetMyFinancialOnboardingStatusQuery),
                // não por cada evento individual.
                logger.LogDebug("Evento de status de conta ignorado (sem transição própria): {Event}", payload.Event);
                break;
        }

        subaccountRepository.Update(subaccount);
        await MarkProcessedAsync(payload, ct);
    }

    private async Task MarkProcessedAsync(AsaasAccountStatusPayload payload, CancellationToken ct)
    {
        await processedEventRepository.AddAsync(ProcessedAsaasAccountEvent.Create(payload.Id, payload.Event), ct);
        await uow.SaveChangesAsync(ct);
    }
}
