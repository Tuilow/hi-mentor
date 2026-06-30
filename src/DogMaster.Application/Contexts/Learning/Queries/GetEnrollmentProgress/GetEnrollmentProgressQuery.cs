using MediatR;

namespace DogMaster.Application.Contexts.Learning.Queries.GetEnrollmentProgress;

public sealed record GetEnrollmentProgressQuery(Guid UserId, Guid CourseId)
    : IRequest<EnrollmentProgressResponse?>;
