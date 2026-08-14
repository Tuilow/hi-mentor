namespace HiMentor.Catalog.Application.Queries.GetCourseBySlug;

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
    string? GuaranteeText,
    // Estado real de comercialização ("Free"/"Paid"/"Subscription"/"Hidden") — ver
    // CourseCommercializationResolver. Price/IsFree seguem existindo (compatibilidade), mas o
    // front-end deve exibir "Grátis"/preço a partir deste campo, nunca derivar de novo.
    string CommercializationState
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
    bool HasVideo,
    // Materiais de apoio anexados via "4. Materiais" do assistente de criação — o dado já
    // existia em Lesson.Attachments desde sempre, só não era exposto por este DTO. Usado tanto
    // pela tela de edição do criador (GetCourseByIdAdminQueryHandler) quanto pela aba
    // "Materiais" do player do aluno (GetCourseBySlugQueryHandler) — mesmo shape, mesma fonte.
    IEnumerable<LessonAttachmentResponse> Attachments
);

public sealed record LessonAttachmentResponse(
    Guid Id,
    string Title,
    string FileUrl,
    string? FileType,
    long? FileSizeBytes
);
