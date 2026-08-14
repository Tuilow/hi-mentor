using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Catalog.Application.Interfaces;
using HiMentor.Catalog.Domain.Interfaces;
using MediatR;

namespace HiMentor.Catalog.Application.Commands.PublishCourse;

public sealed class PublishCourseCommandHandler(
    ICourseRepository courseRepository, ICreatorFinancialStatusLookup financialStatusLookup, IUnitOfWork uow
) : IRequestHandler<PublishCourseCommand>
{
    public async Task Handle(PublishCourseCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o instrutor pode publicar o curso.");

        // Onboarding financeiro (subconta Asaas/BaaS) obrigatório para PUBLICAR/vender -- nunca
        // para criar/editar o curso (item 12 do briefing de onboarding financeiro). Cursos
        // grátis também exigem financeiro aprovado: mesmo sem cobrança, "publicar" é o mesmo
        // ato de torná-lo visível/vendável na vitrine, e a regra do negócio (decisão explícita
        // do dono do produto) é que só falamos em "criador pronto para vender" depois do
        // onboarding -- ver relatório final sobre o corte imediato para todos os criadores.
        if (!await financialStatusLookup.CanSellAsync(course.InstructorId, ct))
            throw new BusinessException(
                "Complete o onboarding financeiro antes de publicar este curso. Acesse Financeiro -> Configurar recebimentos.");

        course.Publish();
        courseRepository.Update(course);
        await uow.SaveChangesAsync(ct);
    }
}
