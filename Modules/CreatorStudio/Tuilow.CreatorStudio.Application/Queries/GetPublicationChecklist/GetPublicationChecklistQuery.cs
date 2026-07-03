using Tuilow.CreatorStudio.Application.Common;
using MediatR;

namespace Tuilow.CreatorStudio.Application.Queries.GetPublicationChecklist;

public sealed record GetPublicationChecklistQuery(Guid CourseId, Guid InstructorId) : IRequest<PublicationChecklistResult>;
