using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Commands.ResendAccessLink;

/// <summary>
/// Reenvia um Magic Link para quem perdeu a janela de 48h do e-mail original pós-compra (ver
/// User.RegisterFromPurchase/IssueMagicLink) e ficaria sem nenhum jeito self-service de
/// entrar, já que a conta nasce sem senha. Mesmo padrão de privacidade de ForgotPasswordCommand
/// — não revela se o e-mail existe ou não na base.
/// </summary>
public sealed record ResendAccessLinkCommand(string Email) : IRequest;
