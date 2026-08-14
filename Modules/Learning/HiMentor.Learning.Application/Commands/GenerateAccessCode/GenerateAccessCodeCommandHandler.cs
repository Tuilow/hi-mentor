using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Catalog.Domain.Enums;
using HiMentor.Catalog.Domain.Interfaces;
using HiMentor.Learning.Domain.Entities;
using HiMentor.Learning.Domain.Interfaces;
using MediatR;

namespace HiMentor.Learning.Application.Commands.GenerateAccessCode;

public sealed class GenerateAccessCodeCommandHandler(
    ICourseRepository courseRepository,
    IAccessCodeRepository accessCodeRepository,
    IUnitOfWork uow
) : IRequestHandler<GenerateAccessCodeCommand, GenerateAccessCodeResult>
{
    public async Task<GenerateAccessCodeResult> Handle(GenerateAccessCodeCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        // Só programas publicados: gerar um código para um rascunho deixaria o aluno com um
        // acesso "fantasma" (nada para acessar de fato até o Creator publicar).
        if (course.Status != CourseStatus.Published)
            throw new BusinessException("Só é possível gerar códigos de acesso para programas publicados.");

        var accessCode = AccessCode.Generate(request.CourseId, request.AdminUserId, request.MaxUses, request.ExpiresAt);
        await accessCodeRepository.AddAsync(accessCode, ct);
        await uow.SaveChangesAsync(ct);

        return new GenerateAccessCodeResult(
            accessCode.Id, accessCode.Code, course.Title, accessCode.MaxUses, accessCode.ExpiresAt, accessCode.CreatedAt);
    }
}
