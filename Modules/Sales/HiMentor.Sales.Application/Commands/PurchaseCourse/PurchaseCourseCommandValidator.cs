using FluentValidation;

namespace HiMentor.Sales.Application.Commands.PurchaseCourse;

public sealed class PurchaseCourseCommandValidator : AbstractValidator<PurchaseCourseCommand>
{
    public PurchaseCourseCommandValidator()
    {
        // StudentId é opcional (checkout anônimo) — quando ausente, CustomerName/CustomerEmail
        // são obrigatórios para localizar ou criar a conta (ver IUserProvisioningService).
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress();
    }
}
