using HiMentor.CreatorStudio.Application.Interfaces;
using MediatR;

namespace HiMentor.CreatorStudio.Application.Queries.GetVideoEditingCapabilities;

public sealed class GetVideoEditingCapabilitiesQueryHandler(
    IVideoEditingService videoEditingService
) : IRequestHandler<GetVideoEditingCapabilitiesQuery, VideoEditingCapabilities>
{
    public Task<VideoEditingCapabilities> Handle(GetVideoEditingCapabilitiesQuery request, CancellationToken ct) =>
        videoEditingService.GetCapabilitiesAsync(ct);
}
