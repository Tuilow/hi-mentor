using MediatR;

namespace HiMentor.Journey.Application.Commands.RegisterLearnerProfile;

public sealed record RegisterLearnerProfileCommand(
    Guid UserId,
    string Name,
    string? Category,
    DateOnly? BirthDate
) : IRequest<Guid>;
