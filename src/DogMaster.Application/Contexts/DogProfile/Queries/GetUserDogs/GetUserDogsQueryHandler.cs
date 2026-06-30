using DogMaster.Domain.Contexts.DogProfile.Interfaces;
using MediatR;

namespace DogMaster.Application.Contexts.DogProfile.Queries.GetUserDogs;

public sealed class GetUserDogsQueryHandler(IDogRepository dogRepository)
    : IRequestHandler<GetUserDogsQuery, IEnumerable<DogResponse>>
{
    public async Task<IEnumerable<DogResponse>> Handle(GetUserDogsQuery request, CancellationToken ct)
    {
        var dogs = await dogRepository.GetByUserAsync(request.UserId, ct);
        return dogs.Select(d => new DogResponse(
            d.Id, d.Name, d.Breed, d.Sex, d.AgeMonths, d.WeightKg, d.PhotoUrl, d.Level.ToString()));
    }
}
