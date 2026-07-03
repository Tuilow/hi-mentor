using MediatR;

namespace Tuilow.CreatorStudio.Application.Commands.CaptureLead;

/// <summary>Formulário de interesse da página de vendas pública — endpoint anônimo.</summary>
public sealed record CaptureLeadCommand(
    Guid CourseId,
    string Name,
    string Email,
    string? Phone,
    string? Source
) : IRequest<Guid>;
