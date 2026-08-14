using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Catalog.Domain.Entities;
using HiMentor.Catalog.Domain.Interfaces;
using MediatR;

namespace HiMentor.Catalog.Application.Commands.DuplicateCourse;

public sealed class DuplicateCourseCommandHandler(
    ICourseRepository courseRepository, IUnitOfWork uow
) : IRequestHandler<DuplicateCourseCommand, Guid>
{
    public async Task<Guid> Handle(DuplicateCourseCommand request, CancellationToken ct)
    {
        var source = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (source.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o criador pode duplicar este produto.");

        // Monta o clone inteiramente em memória, usando a mesma API pública que o assistente
        // de criação usa (Course.Create + AddModule/AddLesson/AddAttachment/...). Como o Course
        // clonado ainda não foi rastreado pelo EF (só entra via AddAsync no final), toda a árvore
        // é adicionada em cascata como Added — dispensa o truque de "Added explícito" usado
        // quando se acrescenta filhos a um agregado JÁ existente/rastreado.
        var clone = Course.Create(
            source.InstructorId, $"{source.Title} (cópia)", source.Description,
            source.Level, source.Price.Amount);

        clone.UpdateBasicInfo(clone.Title, source.Category, source.Subcategory,
            source.ShortDescription, source.Description);
        clone.SetProductType(source.ProductType);
        clone.SetSalesPage(source.SalesPageHeadline, source.SalesPageSubheadline,
            source.SalesPageCtaText, source.SalesPageBenefits,
            source.SalesPageVideoUrl, source.Testimonials,
            source.GuaranteeDays, source.GuaranteeText);

        foreach (var faq in source.FaqItems.OrderBy(f => f.Order))
            clone.AddFaqItem(faq.Question, faq.Answer);

        foreach (var module in source.Modules.OrderBy(m => m.Order))
        {
            var newModule = clone.AddModule(module.Title, module.Description);

            foreach (var lesson in module.Lessons.OrderBy(l => l.Order))
            {
                var newLesson = newModule.AddLesson(lesson.Title, lesson.Description, lesson.IsPreview);

                if (lesson.VideoId.HasValue)
                    newLesson.SetVideo(lesson.VideoId.Value, lesson.DurationSeconds ?? 0);

                foreach (var attachment in lesson.Attachments)
                    newLesson.AddAttachment(attachment.Title, attachment.FileUrl, attachment.FileType, attachment.FileSizeBytes);

                foreach (var exercise in lesson.Exercises)
                    newLesson.AddExercise(exercise.Title, exercise.Description);
            }
        }

        await courseRepository.AddAsync(clone, ct);
        await uow.SaveChangesAsync(ct);
        return clone.Id;
    }
}
