using Tuilow.SharedKernel.Domain.Common;
using Tuilow.Streaming.Domain.Enums;

namespace Tuilow.Streaming.Domain.Entities;

public sealed class Video : AggregateRoot
{
    public string? CloudflareVideoId { get; private set; }
    public VideoStatus Status { get; private set; } = VideoStatus.Uploading;
    public int? DurationSeconds { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public long? SizeBytes { get; private set; }
    public bool IsProtected { get; private set; } = true;
    public DateTime? ReadyAt { get; private set; }

    private Video() { }

    public static Video Create() => new();

    public void SetCloudflareVideoId(string videoId)
    {
        CloudflareVideoId = videoId;
        Status = VideoStatus.Processing;
        Touch();
    }

    public void MarkReady(int durationSeconds, string? thumbnailUrl = null)
    {
        Status = VideoStatus.Ready;
        DurationSeconds = durationSeconds;
        ThumbnailUrl = thumbnailUrl;
        ReadyAt = DateTime.UtcNow;
        Touch();
    }

    public void MarkError()
    {
        Status = VideoStatus.Error;
        Touch();
    }
}
