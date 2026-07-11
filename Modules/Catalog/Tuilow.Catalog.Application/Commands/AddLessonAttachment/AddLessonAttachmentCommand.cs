using MediatR;

namespace Tuilow.Catalog.Application.Commands.AddLessonAttachment;

/// <summary>Passo 4 do assistente ("Materiais") — anexa um arquivo (PDF/DOCX/PPTX/ZIP/imagem/planilha) à aula.</summary>
public sealed record AddLessonAttachmentCommand(
    Guid CourseId,
    Guid InstructorId,
    Guid ModuleId,
    Guid LessonId,
    string Title,
    string FileUrl,
    string? FileType,
    long? FileSizeBytes
) : IRequest<Guid>;
