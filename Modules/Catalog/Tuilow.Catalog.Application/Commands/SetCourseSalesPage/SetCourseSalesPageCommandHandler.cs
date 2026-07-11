using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.Entities;
using Tuilow.Catalog.Domain.Interfaces;
using MediatR;

namespace Tuilow.Catalog.Application.Commands.SetCourseSalesPage;

public sealed class SetCourseSalesPageCommandHandler(
    ICourseRepository courseRepository, IUnitOfWork uow
) : IRequestHandler<SetCourseSalesPageCommand>
{
    public async Task Handle(SetCourseSalesPageCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o criador pode editar a página de vendas.");

        course.SetSalesPage(
            request.Headline, request.Subheadline, request.CtaText, request.Benefits,
            request.VideoUrl,
            request.Testimonials?.Select(t => new Testimonial(t.AuthorName, t.AuthorRole, t.Quote, t.AvatarUrl)),
            request.GuaranteeDays, request.GuaranteeText);
        courseRepository.Update(course);

        // FAQ: substitui a lista inteira. Remove explicitamente as linhas antigas do
        // DbContext (senão ficariam órfãs no banco) antes de limpar a coleção em memória e
        // adicionar as novas — que têm Guid novo e precisam ser explicitamente Added (mesmo
        // padrão de Module/Lesson: o Course já está tracked/Modified).
        foreach (var oldItem in course.FaqItems.ToList())
            courseRepository.RemoveFaqItem(oldItem);

        course.ClearFaqItems();
        if (request.FaqItems is not null)
        {
            foreach (var faq in request.FaqItems)
            {
                var item = course.AddFaqItem(faq.Question, faq.Answer);
                await courseRepository.AddFaqItemAsync(item, ct);
            }
        }

        await uow.SaveChangesAsync(ct);
    }
}
