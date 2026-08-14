using HiMentor.Catalog.Domain.Interfaces;
using HiMentor.IdentidadeAcesso.Domain.Events;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Streaming.Application.Interfaces;
using HiMentor.Streaming.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiMentor.Streaming.Application.EventHandlers;

/// <summary>
/// Achado M11 da auditoria de arquitetura: reage à exclusão de uma conta de criador
/// (User.MarkDeleted, módulo IdentidadeAcesso) apagando os vídeos dos cursos dele — registro
/// local e arquivo no Cloudflare Stream. Antes desta correção, essa exclusão em cascata vivia
/// dentro de IdentidadeAcesso.DeleteUserCommandHandler, que por isso precisava referenciar
/// HiMentor.Streaming.Application (IStreamingService) diretamente — a única referência
/// Application-to-Application entre módulos do repositório. Movida para cá porque "apagar um
/// vídeo direito" (registro local + Cloudflare) é uma responsabilidade do módulo Streaming, não
/// de IdentidadeAcesso — o mesmo raciocínio já usado em DeleteVideoCommandHandler (exclusão
/// avulsa de um vídeo pelo próprio criador).
///
/// Roda DEPOIS que a exclusão da conta já foi commitada (ver AppDbContext.SaveChangesAsync: salva
/// primeiro, despacha os domain events depois) — não é a mesma transação, então tem seu próprio
/// SaveChanges. courseRepository.ListByInstructorAsync devolve cursos em qualquer status
/// (inclusive já arquivados por DeleteUserCommandHandler na mesma exclusão), então funciona
/// independente da ordem entre o arquivamento do curso e este handler.
/// </summary>
public sealed class UserDeletedEventHandler(
    ICourseRepository courseRepository,
    IVideoRepository videoRepository,
    IStreamingService streamingService,
    IUnitOfWork uow,
    ILogger<UserDeletedEventHandler> logger
) : INotificationHandler<UserDeletedDomainEvent>
{
    public async Task Handle(UserDeletedDomainEvent notification, CancellationToken ct)
    {
        var courses = await courseRepository.ListByInstructorAsync(notification.UserId, ct);
        var anyVideoDeleted = false;

        foreach (var course in courses)
        {
            var videos = await videoRepository.ListByCourseAsync(course.Id, ct);
            foreach (var video in videos)
            {
                // Melhor esforço: uma falha no Cloudflare não pode travar a limpeza do registro
                // local — o pior caso é um arquivo órfão sendo cobrado no Cloudflare, não dados
                // inconsistentes no banco. Mesmo padrão de DeleteVideoCommandHandler.
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
                            video.CloudflareVideoId, video.Id, notification.UserId);
                    }
                }

                videoRepository.Delete(video);
                anyVideoDeleted = true;
            }
        }

        if (anyVideoDeleted)
            await uow.SaveChangesAsync(ct);
    }
}
