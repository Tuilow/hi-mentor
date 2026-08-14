using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Catalog.Domain.Enums;
using HiMentor.Catalog.Domain.Interfaces;
using MediatR;

namespace HiMentor.Catalog.Application.Commands.DeleteCourse;

public sealed class DeleteCourseCommandHandler(
    ICourseRepository courseRepository, IUnitOfWork uow
) : IRequestHandler<DeleteCourseCommand>
{
    public async Task Handle(DeleteCourseCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o criador pode excluir este produto.");

        // Produto Publicado (ou já Arquivado) pode ter alunos matriculados/compras associadas —
        // excluir de verdade quebraria acesso e histórico financeiro. Nesses casos o caminho é
        // arquivar (ArchiveCourse), não excluir. Só Draft/InReview (nunca foram ao ar) podem
        // ser excluídos de fato.
        if (course.Status is CourseStatus.Published or CourseStatus.Archived)
            throw new BusinessException(
                "Produtos publicados não podem ser excluídos, apenas arquivados, para preservar o acesso de alunos e o histórico de vendas.");

        courseRepository.Delete(course);
        await uow.SaveChangesAsync(ct);
    }
}
