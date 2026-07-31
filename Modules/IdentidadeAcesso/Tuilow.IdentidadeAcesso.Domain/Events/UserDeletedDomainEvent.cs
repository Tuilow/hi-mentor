using Tuilow.SharedKernel.Domain.Common;

namespace Tuilow.IdentidadeAcesso.Domain.Events;

/// <summary>
/// Achado M11 da auditoria de arquitetura: levantado por User.MarkDeleted() (exclusão de conta via
/// painel do dono da plataforma). Consumido por Tuilow.Streaming.Application.EventHandlers.UserDeletedEventHandler,
/// que apaga os vídeos dos cursos do criador excluído (registro local + Cloudflare Stream).
///
/// Antes desta correção, essa exclusão em cascata de vídeos vivia dentro de
/// IdentidadeAcesso.DeleteUserCommandHandler, que por isso precisava referenciar
/// Tuilow.Streaming.Application (IStreamingService) diretamente — a única referência
/// Application-to-Application entre módulos de todo o repositório (o resto do sistema só
/// referencia Domain-to-Domain entre módulos). Mover para um domain event elimina essa referência:
/// IdentidadeAcesso.Application não precisa mais saber nada sobre Streaming além do necessário
/// para o painel administrativo (contagem de vídeos, via IVideoRepository — Domain).
/// </summary>
public sealed record UserDeletedDomainEvent(Guid UserId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
