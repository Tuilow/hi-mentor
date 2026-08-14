using HiMentor.CreatorStudio.Application.Common;
using MediatR;

namespace HiMentor.CreatorStudio.Application.Queries.GetPublicationChecklist;

public sealed record GetPublicationChecklistQuery(Guid CourseId, Guid InstructorId) : IRequest<PublicationChecklistResult>;
