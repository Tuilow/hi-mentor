using HiMentor.Channel.Domain.Interfaces;
using MediatR;

namespace HiMentor.Channel.Application.Queries.GetMyChannel;

public sealed class GetMyChannelQueryHandler(ICreatorChannelRepository channelRepository)
    : IRequestHandler<GetMyChannelQuery, MyChannelResponse?>
{
    public async Task<MyChannelResponse?> Handle(GetMyChannelQuery request, CancellationToken ct)
    {
        var channel = await channelRepository.GetByCreatorIdAsync(request.CreatorId, ct);
        if (channel is null) return null;

        return new MyChannelResponse(
            channel.Id, channel.Handle.Value,
            channel.SocialLinks.Select(l => new SocialLinkResponse(l.Platform, l.Url)).ToList(),
            channel.BannerUrl, channel.IntroVideoUrl);
    }
}
