using Tuilow.Journey.Application.Commands.RegisterLearnerProfile;
using Tuilow.Journey.Application.Queries.GetUserLearnerProfiles;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.Journey.Api.Controllers;

[ApiController]
[Route("api/v1/learner-profiles")]
[Authorize]
[Produces("application/json")]
public sealed class LearnerProfilesController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Lista todos os perfis de aprendizado do usuário autenticado.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMyProfiles(CancellationToken ct)
    {
        var profiles = await sender.Send(new GetUserLearnerProfilesQuery(currentUser.UserId!.Value), ct);
        return Ok(profiles);
    }

    /// <summary>Registra um novo perfil de aprendizado para o usuário autenticado.</summary>
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterLearnerProfileRequest request, CancellationToken ct)
    {
        var profileId = await sender.Send(new RegisterLearnerProfileCommand(
            currentUser.UserId!.Value, request.Name, request.Category, request.BirthDate), ct);
        return Ok(new { profileId });
    }
}

public sealed record RegisterLearnerProfileRequest(
    string Name,
    string? Category,
    DateOnly? BirthDate
);
