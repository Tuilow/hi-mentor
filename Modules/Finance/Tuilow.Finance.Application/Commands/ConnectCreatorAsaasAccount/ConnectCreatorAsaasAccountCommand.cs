using MediatR;

namespace Tuilow.Finance.Application.Commands.ConnectCreatorAsaasAccount;

/// <summary>
/// Conecta (ou reconecta) a conta Asaas propria de um creator ao marketplace de split. O
/// creator gera a API Key na propria conta Asaas dele (fora da Tuilow) e cola aqui uma unica
/// vez -- a Asaas so exibe o valor no momento da criacao. Ver CreatorAsaasAccount para o
/// racional completo de por que este e um modelo "traga sua propria conta" em vez de subconta
/// criada pela Tuilow via API.
/// </summary>
public sealed record ConnectCreatorAsaasAccountCommand(
    Guid CreatorId, string ApiKey, string? CpfCnpj, string? LegalName
) : IRequest<ConnectCreatorAsaasAccountResult>;

public sealed record ConnectCreatorAsaasAccountResult(bool Success, string Status, string? ErrorMessage);
