using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.CreatorStudio.Application.Common;
using MediatR;

namespace Tuilow.CreatorStudio.Application.Commands.PublishProduct;

/// <summary>
/// Orquestra a publicação: revalida o checklist no servidor (nunca confia no que o front
/// mostrou) e então delega a regra de negócio real para o próprio agregado Course
/// (<see cref="Tuilow.Catalog.Domain.Entities.Course.Publish"/>) — mesma validação de
/// módulo/aula que o endpoint de publish do Catalog já usa, sem duplicá-la.
/// </summary>
public sealed class PublishProductCommandHandler(
    ICourseRepository courseRepository, IUnitOfWork uow
) : IRequestHandler<PublishProductCommand>
{
    public async Task Handle(PublishProductCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o criador pode publicar este produto.");

        var checklist = PublicationChecklist.Evaluate(course);
        if (!checklist.IsComplete)
            throw new BusinessException(
                "Complete o checklist de publicação antes de publicar: dados básicos, conteúdo (vídeo), preço e página de vendas.");

        course.Publish(); // reaproveita a validação de módulo/aula já existente no domínio
        courseRepository.Update(course);
        await uow.SaveChangesAsync(ct);
    }
}
