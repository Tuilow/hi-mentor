using Tuilow.IdentidadeAcesso.Application.Common;
using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Commands.ConsumeMagicLink;

/// <summary>
/// Troca um Magic Link (token de uso único enviado por e-mail/WhatsApp após pagamento
/// confirmado) por um AuthTokens normal — mesmo formato de login/registro, para o front tratar
/// exatamente como qualquer outro login já feito.
/// </summary>
public sealed record ConsumeMagicLinkCommand(string Token, string? IpAddress = null) : IRequest<AuthTokens>;
