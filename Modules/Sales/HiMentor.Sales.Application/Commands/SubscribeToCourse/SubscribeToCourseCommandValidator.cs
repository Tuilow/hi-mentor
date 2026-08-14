using FluentValidation;

namespace HiMentor.Sales.Application.Commands.SubscribeToCourse;

public sealed class SubscribeToCourseCommandValidator : AbstractValidator<SubscribeToCourseCommand>
{
    public SubscribeToCourseCommandValidator()
    {
        // UserId é opcional (checkout anônimo) — quando ausente, CustomerName/CustomerEmail são
        // obrigatórios para localizar ou criar a conta (ver IUserProvisioningService). Mesmo
        // padrão de PurchaseCourseCommandValidator (compra avulsa).
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress();
    }
}
