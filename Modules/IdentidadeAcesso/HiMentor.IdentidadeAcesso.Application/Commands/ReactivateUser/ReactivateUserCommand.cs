using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Commands.ReactivateUser;

/// <summary>Reverte uma suspensão (ou reativa uma conta previamente excluída) pelo painel do dono da plataforma.</summary>
public sealed record ReactivateUserCommand(Guid TargetUserId) : IRequest;
