using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.CreatorStudio.Application.Common;
using Tuilow.Finance.Domain.Interfaces;
using MediatR;

namespace Tuilow.CreatorStudio.Application.Commands.PublishProduct;

/// <summary>
/// Orquestra a publicação: revalida o checklist no servidor (nunca confia no que o front
/// mostrou) e então delega a regra de negócio real para o próprio agregado Course
/// (<see cref="Tuilow.Catalog.Domain.Entities.Course.Publish"/>) — mesma validação de
/// módulo/aula que o endpoint de publish do Catalog já usa, sem duplicá-la.
///
/// Consulta ICreatorAsaasSubaccountRepository (Finance.Domain) direto -- mesmo padrão de
/// acoplamento já documentado no csproj deste projeto ("orquestração... só lê pelos
/// repositórios já existentes de cada módulo") -- para exigir onboarding financeiro aprovado
/// antes de publicar (item 12 do briefing de onboarding financeiro; mesma regra também
/// aplicada, de forma independente, em Catalog.PublishCourseCommandHandler para o caso de
/// publicação direta fora do assistente).
/// </summary>
public sealed class PublishProductCommandHandler(
    ICourseRepository courseRepository, ICreatorAsaasSubaccountRepository financialAccountRepository, IUnitOfWork uow
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

        var financialAccount = await financialAccountRepository.GetByCreatorIdAsync(course.InstructorId, ct);
        if (financialAccount is not { CanSell: true })
            throw new BusinessException(
                "Complete o onboarding financeiro antes de publicar. Acesse Financeiro -> Configurar recebimentos.");

        course.Publish(); // reaproveita a validação de módulo/aula já existente no domínio
        courseRepository.Update(course);
        await uow.SaveChangesAsync(ct);
    }
}
