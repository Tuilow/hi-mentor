using HiMentor.SharedKernel.Domain.Common;

namespace HiMentor.Catalog.Domain.Entities;

public sealed class LessonAttachment : Entity
{
    public Guid LessonId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string FileUrl { get; private set; } = string.Empty;
    public string? FileType { get; private set; }
    public long? FileSizeBytes { get; private set; }

    private LessonAttachment() { }

    public static LessonAttachment Create(Guid lessonId, string title, string fileUrl,
        string? fileType = null, long? fileSizeBytes = null) =>
        new()
        {
            LessonId = lessonId,
            Title = title.Trim(),
            FileUrl = fileUrl,
            FileType = fileType,
            FileSizeBytes = fileSizeBytes
        };
}
