using DogMaster.Domain.Common.Interfaces;
using DogMaster.Domain.Contexts.DogProfile.Entities;
using DogMaster.Domain.Contexts.DogProfile.Interfaces;
using MediatR;

namespace DogMaster.Application.Contexts.DogProfile.Commands.RegisterDog;

public sealed class RegisterDogCommandHandler(
    IDogRepository dogRepository, IUnitOfWork uow
) : IRequestHandler<RegisterDogCommand, Guid>
{
    public async Task<Guid> Handle(RegisterDogCommand request, CancellationToken ct)
    {
        var dog = Dog.Create(request.UserId, request.Name, request.Breed,
            request.Sex, request.BirthDate, request.WeightKg);

        await dogRepository.AddAsync(dog, ct);
        await uow.SaveChangesAsync(ct);
        return dog.Id;
    }
}
