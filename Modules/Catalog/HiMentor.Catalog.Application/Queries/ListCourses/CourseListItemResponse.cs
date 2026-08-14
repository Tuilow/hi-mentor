namespace HiMentor.Catalog.Application.Queries.ListCourses;

public sealed record CourseListItemResponse(
    Guid Id,
    string Title,
    string Slug,
    string? ShortDescription,
    string? ThumbnailUrl,
    decimal Price,
    bool IsFree,
    string Level,
    int TotalDurationMinutes,
    DateTime? PublishedAt,
    string? Category,
    string ProductType,
    // Estado real de comercialização ("Free"/"Paid"/"Subscription"/"Hidden") — ver
    // CourseCommercializationResolver. Price/IsFree seguem existindo (compatibilidade), mas o
    // front-end deve exibir "Grátis"/preço a partir deste campo, nunca derivar de novo.
    string CommercializationState
);
