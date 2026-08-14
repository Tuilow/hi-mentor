using HiMentor.IdentidadeAcesso.Domain.Interfaces;
using HiMentor.IdentidadeAcesso.Domain.Enums;
using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Catalog.Domain.Interfaces;
using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Commands.DeleteUser;

/// <summary>
/// Referencia ICourseRepository (Catalog) diretamente — mesmo padrão de acoplamento
/// Domain-to-Domain entre módulos já usado por DeleteVideoCommandHandler (Streaming referenciando
/// ICourseRepository do Catalog).
///
/// Achado M11 da auditoria de arquitetura (CORRIGIDO): até aqui, este handler também percorria
/// IVideoRepository (Streaming.Domain) e chamava IStreamingService (HiMentor.Streaming.Application)
/// para apagar os vídeos do criador excluído — essa última era a única referência
/// Application-to-Application entre módulos de todo o repositório. A exclusão em cascata de
/// vídeos (registro local + Cloudflare Stream) foi movida inteira para
/// HiMentor.Streaming.Application.EventHandlers.UserDeletedEventHandler, reagindo ao
/// UserDeletedDomainEvent levantado por User.MarkDeleted() abaixo — "apagar um vídeo direito" é
/// responsabilidade do módulo Streaming, não de IdentidadeAcesso. O arquivamento de cursos
/// continua aqui: é uma operação simples sobre Course (Catalog.Domain), sem a complexidade de
/// external service que motivou mover a parte de vídeos.
/// </summary>
public sealed class DeleteUserCommandHandler(
    IUserRepository userRepository,
    ICourseRepository courseRepository,
    IUnitOfWork uow
) : IRequestHandler<DeleteUserCommand>
{
    public async Task Handle(DeleteUserCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(request.TargetUserId, ct)
            ?? throw new NotFoundException("Usuário", request.TargetUserId);

        // Trava de segurança simples: não deixa excluir uma conta de administrador por aqui
        // (evita o dono se autoexcluir ou travar o próprio acesso ao painel por engano).
        if (user.HasRole(RoleNames.Admin))
            throw new BusinessException("Não é possível excluir uma conta de administrador por aqui — remova o role Admin primeiro.");

        // Arquiva em vez de apagar: some da loja pública, mas o registro fica preservado para
        // quem já comprou o curso continuar com acesso à página (só os vídeos somem — ver
        // UserDeletedEventHandler no módulo Streaming, disparado pelo MarkDeleted() abaixo).
        var courses = await courseRepository.ListByInstructorAsync(user.Id, ct);
        foreach (var course in courses)
            course.Archive();

        user.MarkDeleted();
        await uow.SaveChangesAsync(ct);
    }
}
