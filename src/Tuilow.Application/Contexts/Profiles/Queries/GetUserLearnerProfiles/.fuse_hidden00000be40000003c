using MediatR;

namespace DogMaster.Application.Contexts.DogProfile.Queries.GetUserDogs;

public sealed record GetUserDogsQuery(Guid UserId) : IRequest<IEnumerable<DogResponse>>;

public sealed record DogResponse(
    Guid Id, string Name, string? Breed, string? Sex,
    int? AgeMonths, decimal? WeightKg, string? PhotoUrl, string Level
);
