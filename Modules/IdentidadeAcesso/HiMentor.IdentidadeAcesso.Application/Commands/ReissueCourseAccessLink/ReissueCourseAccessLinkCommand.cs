using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Commands.ReissueCourseAccessLink;

/// <summary>
/// Reemite (gera um novo) Magic Link de acesso a um curso especifico -- usado pelo painel
/// administrativo quando o e-mail original de liberacao de acesso nao chegou ao aluno (foi pro
/// spam, falhou o envio, etc.). Nunca reaproveita/expoe um token ja existente: sempre emite um
/// novo, do mesmo jeito que CoursePurchaseConfirmedEventHandler faz ao liberar o acesso pela
/// primeira vez -- ver ReissueCourseAccessLinkCommandHandler.
/// </summary>
public sealed record ReissueCourseAccessLinkCommand(Guid AdminUserId, Guid StudentUserId, Guid CourseId)
    : IRequest<ReissueCourseAccessLinkResult>;

public sealed record ReissueCourseAccessLinkResult(string AccessUrl);
