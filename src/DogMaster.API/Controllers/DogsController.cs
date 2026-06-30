using DogMaster.Application.Contexts.DogProfile.Commands.RegisterDog;
using DogMaster.Application.Contexts.DogProfile.Queries.GetUserDogs;
using DogMaster.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogMaster.API.Controllers;

[ApiController]
[Route("api/v1/dogs")]
[Authorize]
[Produces("application/json")]
public sealed class DogsController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Lista todos os cães do usuário autenticado.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMyDogs(CancellationToken ct)
    {
        var dogs = await sender.Send(new GetUserDogsQuery(currentUser.UserId!.Value), ct);
        return Ok(dogs);
    }

    /// <summary>Registra um novo cão para o usuário autenticado.</summary>
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterDogRequest request, CancellationToken ct)
    {
        var dogId = await sender.Send(new RegisterDogCommand(
            currentUser.UserId!.Value, request.Name, request.Breed,
            request.Sex, request.BirthDate, request.WeightKg, request.IsNeutered), ct);
        return Ok(new { dogId });
    }
}

public sealed record RegisterDogRequest(
    string Name,
    string? Breed,
    string? Sex,
    DateOnly? BirthDate,
    decimal? WeightKg,
    bool? IsNeutered
);
