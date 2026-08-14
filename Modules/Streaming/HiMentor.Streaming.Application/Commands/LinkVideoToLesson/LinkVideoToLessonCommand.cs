using MediatR;

namespace HiMentor.Streaming.Application.Commands.LinkVideoToLesson;

/// <summary>
/// Vincula um vídeo (já enviado ao Cloudflare) a uma aula de um módulo.
/// IsPreview = true  → aula gratuita, qualquer visitante pode assistir
/// IsPreview = false → requer assinatura ativa para assistir
/// </summary>
public sealed record LinkVideoToLessonCommand(
    Guid CourseId,
    Guid InstructorId,
    Guid ModuleId,
    Guid LessonId,
    Guid VideoId,
    bool IsPreview = false
) : IRequest;
