namespace Tuilow.Catalog.Application.Queries.GetCourseBySlug;

public sealed record CourseDetailResponse(
    Guid Id,
    string Title,
    string Slug,
    string Description,
    string? ShortDescription,
    string? ThumbnailUrl,
    decimal Price,
    bool IsFree,
    string Level,
    int TotalDurationMinutes,
    DateTime? PublishedAt,
    IEnumerable<ModuleResponse> Modules,
    string Status,
    string? Category,
    string? Subcategory,
    string ProductType,
    int ViewCount,
    string? SalesPageHeadline,
    string? SalesPageSubheadline,
    string? SalesPageCtaText,
    IEnumerable<string> SalesPageBenefits,
    IEnumerable<FaqItemResponse> FaqItems,
    Guid InstructorId,
    string? InstructorName,
    string? InstructorAvatarUrl,
    string? InstructorBio,
    string? SalesPageVideoUrl,
    IEnumerable<TestimonialResponse> Testimonials,
    int? GuaranteeDays,
    string? GuaranteeText
);

public sealed record FaqItemResponse(
    Guid Id,
    string Question,
    string Answer,
    int Order
);

public sealed record TestimonialResponse(
    string AuthorName,
    string? AuthorRole,
    string Quote,
    string? AvatarUrl
);

public sealed record ModuleResponse(
    Guid Id,
    string Title,
    string? Description,
    int Order,
    IEnumerable<LessonResponse> Lessons
);

public sealed record LessonResponse(
    Guid Id,
    string Title,
    string? Description,
    int Order,
    int? DurationSeconds,
    bool IsPreview,
    bool HasVideo
);
