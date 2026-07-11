using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.Interfaces;
using MediatR;

namespace Tuilow.Catalog.Application.Commands.RecordCourseView;

public sealed class RecordCourseViewCommandHandler(
    ICourseRepository courseRepository, IUnitOfWork uow
) : IRequestHandler<RecordCourseViewCommand>
{
    public async Task Handle(RecordCourseViewCommand request, CancellationToken ct)
    {
        // Best-effort: página de venda inexistente/removida não deve gerar erro pro visitante.
        var course = await courseRepository.GetBySlugAsync(request.Slug, ct);
        if (course is null) return;

        course.IncrementViewCount();
        courseRepository.Update(course);
        await uow.SaveChangesAsync(ct);
    }
}
