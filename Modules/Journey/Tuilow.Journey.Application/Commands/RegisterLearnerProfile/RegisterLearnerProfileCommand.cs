using MediatR;

namespace Tuilow.Journey.Application.Commands.RegisterLearnerProfile;

public sealed record RegisterLearnerProfileCommand(
    Guid UserId,
    string Name,
    string? Category,
    DateOnly? BirthDate
) : IRequest<Guid>;
