using Tuilow.SharedKernel.Application.Common;
using Tuilow.Catalog.Domain.Enums;
using MediatR;

namespace Tuilow.Catalog.Application.Queries.ListCourses;

public sealed record ListCoursesQuery(
    CourseLevel? Level = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 12
) : IRequest<PagedList<CourseListItemResponse>>;
