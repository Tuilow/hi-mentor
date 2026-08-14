using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Catalog.Domain.Interfaces;
using MediatR;

namespace HiMentor.Catalog.Application.Commands.AddLessonAttachment;

public sealed class AddLessonAttachmentCommandHandler(
    ICourseRepository courseRepository, IUnitOfWork uow
) : IRequestHandler<AddLessonAttachmentCommand, Guid>
{
    public async Task<Guid> Handle(AddLessonAttachmentCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o criador pode anexar materiais a este produto.");

        var module = course.Modules.SingleOrDefault(m => m.Id == request.ModuleId)
            ?? throw new NotFoundException("Módulo", request.ModuleId);

        var lesson = module.Lessons.SingleOrDefault(l => l.Id == request.LessonId)
            ?? throw new NotFoundException("Aula", request.LessonId);

        var attachment = lesson.AddAttachment(request.Title, request.FileUrl, request.FileType, request.FileSizeBytes);

        // Registra explicitamente como Added — evita DbUpdateConcurrencyException
        await courseRepository.AddAttachmentAsync(attachment, ct);

        await uow.SaveChangesAsync(ct);
        return attachment.Id;
    }
}
