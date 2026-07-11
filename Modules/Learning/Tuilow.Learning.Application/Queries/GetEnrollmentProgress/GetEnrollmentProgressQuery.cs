using MediatR;

namespace Tuilow.Learning.Application.Queries.GetEnrollmentProgress;

public sealed record GetEnrollmentProgressQuery(Guid UserId, Guid CourseId)
    : IRequest<EnrollmentProgressResponse?>;
