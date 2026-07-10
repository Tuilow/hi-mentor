using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Channel.Domain.Entities;
using Tuilow.Channel.Domain.Interfaces;
using MediatR;

namespace Tuilow.Channel.Application.Commands.UpsertChannel;

public sealed class UpsertChannelCommandHandler(
    ICreatorChannelRepository channelRepository,
    IUnitOfWork uow
) : IRequestHandler<UpsertChannelCommand, Guid>
{
    public async Task<Guid> Handle(UpsertChannelCommand request, CancellationToken ct)
    {
        var links = request.SocialLinks.Select(l => new SocialLink(l.Platform, l.Url));
        var channel = await channelRepository.GetByCreatorIdAsync(request.CreatorId, ct);

        if (channel is null)
        {
            if (await channelRepository.HandleExistsAsync(request.Handle, null, ct))
                throw new BusinessException("Esse @ já está em uso por outro criador. Escolha outro.");

            channel = CreatorChannel.Create(request.CreatorId, request.Handle);
            channel.SetSocialLinks(links);
            await channelRepository.AddAsync(channel, ct);
        }
        else
        {
            if (!string.Equals(channel.Handle.Value, request.Handle.Trim().ToLowerInvariant().TrimStart('@'), StringComparison.Ordinal)
                && await channelRepository.HandleExistsAsync(request.Handle, channel.Id, ct))
                throw new BusinessException("Esse @ já está em uso por outro criador. Escolha outro.");

            channel.ChangeHandle(request.Handle);
            channel.SetSocialLinks(links);
            channelRepository.Update(channel);
        }

        await uow.SaveChangesAsync(ct);
        return channel.Id;
    }
}
