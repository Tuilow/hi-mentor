using MediatR;

namespace Tuilow.Catalog.Application.Commands.UpdateLesson;

/// <summary>Edita título/descrição de uma aula já existente (passo 3 do assistente — usado
/// principalmente para preencher a descrição depois que a aula já foi criada/vinculada a um
/// vídeo, já que AddLesson só pede a descrição no momento da criação).</summary>
public sealed record UpdateLessonCommand(
    Guid CourseId,
    Guid InstructorId,
    Guid ModuleId,
    Guid LessonId,
    string Title,
    string? Description
) : IRequest;
