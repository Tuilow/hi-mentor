using HiMentor.SharedKernel.Application.Common;
using HiMentor.Catalog.Domain.Enums;
using MediatR;

namespace HiMentor.Catalog.Application.Queries.ListCourses;

public sealed record ListCoursesQuery(
    CourseLevel? Level = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 12
) : IRequest<PagedList<CourseListItemResponse>>;
