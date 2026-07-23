using Tuilow.IdentidadeAcesso.Domain.Interfaces;
using Tuilow.IdentidadeAcesso.Domain.Enums;
using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Streaming.Domain.Interfaces;
using Tuilow.Streaming.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Tuilow.IdentidadeAcesso.Application.Commands.DeleteUser;

/// <summary>
/// Referencia ICourseRepository (Catalog) e IVideoRepository/IStreamingService (Streaming)
/// diretamente — mesmo padrão de acoplamento entre módulos já usado por
/// DeleteVideoCommandHandler (Streaming referenciando ICourseRepository do Catalog).
/// </summary>
public sealed class DeleteUserCommandHandler(
    IUserRepository userRepository,
    ICourseRepository courseRepository,
    IVideoRepository videoRepository,
    IStreamingService streamingService,
    IUnitOfWork uow,
    ILogger<DeleteUserCommandHandler> logger
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

        var courses = await courseRepository.ListByInstructorAsync(user.Id, ct);
        foreach (var course in courses)
        {
            var videos = await videoRepository.ListByCourseAsync(course.Id, ct);
            foreach (var video in videos)
            {
                // Mesmo padrão de melhor esforço do DeleteVideoCommandHandler: uma falha no
                // Cloudflare não pode travar a exclusão da conta inteira.
                if (!string.IsNullOrEmpty(video.CloudflareVideoId))
                {
                    try
                    {
                        await streamingService.DeleteVideoAsync(video.CloudflareVideoId, ct);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex,
                            "Falha ao remover o vídeo {CloudflareVideoId} do Cloudflare Stream (Video {VideoId}) durante exclusão da conta {UserId} — removendo só o registro local.",
                            video.CloudflareVideoId, video.Id, user.Id);
                    }
                }

                videoRepository.Delete(video);
            }

            // Arquiva em vez de apagar: some da loja pública, mas o registro fica preservado
            // para quem já comprou o curso continuar com acesso à página (só os vídeos somem).
            course.Archive();
        }

        user.MarkDeleted();
        await uow.SaveChangesAsync(ct);
    }
}
