using MediatR;

namespace Tuilow.Application.Contexts.Profiles.Commands.RegisterLearnerProfile;

public sealed record RegisterLearnerProfileCommand(
    Guid UserId,
    string Name,
    string? Category,
    DateOnly? BirthDate
) : IRequest<Guid>;
