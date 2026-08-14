using MediatR;

namespace HiMentor.Catalog.Application.Commands.UpdateCourseBasicInfo;

/// <summary>Passo 1 do wizard (Info Básica) — também usado pela edição posterior do produto.</summary>
public sealed record UpdateCourseBasicInfoCommand(
    Guid CourseId,
    Guid InstructorId,
    string Title,
    string? Category,
    string? Subcategory,
    string? ShortDescription,
    string Description
) : IRequest;
