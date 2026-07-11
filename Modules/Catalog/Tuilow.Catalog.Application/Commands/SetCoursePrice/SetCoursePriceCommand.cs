using MediatR;

namespace Tuilow.Catalog.Application.Commands.SetCoursePrice;

/// <summary>Passo 5 do assistente ("Preço") — opções Grátis ou Pagamento único.</summary>
public sealed record SetCoursePriceCommand(Guid CourseId, Guid InstructorId, decimal Price) : IRequest;
