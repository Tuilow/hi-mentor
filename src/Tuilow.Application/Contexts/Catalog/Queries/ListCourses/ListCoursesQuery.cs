using Tuilow.Application.Common.Models;
using Tuilow.Domain.Contexts.Catalog.Enums;
using MediatR;

namespace Tuilow.Application.Contexts.Catalog.Queries.ListCourses;

public sealed record ListCoursesQuery(
    CourseLevel? Level = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 12
) : IRequest<PagedList<CourseListItemResponse>>;
