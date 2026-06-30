using MediatR;

namespace DogMaster.Application.Contexts.DogProfile.Commands.RegisterDog;

public sealed record RegisterDogCommand(
    Guid UserId,
    string Name,
    string? Breed,
    string? Sex,
    DateOnly? BirthDate,
    decimal? WeightKg,
    bool? IsNeutered
) : IRequest<Guid>;
