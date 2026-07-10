using Tuilow.Catalog.Domain.Enums;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Channel.Application.Interfaces;
using Tuilow.Channel.Domain.Interfaces;
using Tuilow.Learning.Domain.Interfaces;
using MediatR;

namespace Tuilow.Channel.Application.Queries.GetPublicChannel;

public sealed class GetPublicChannelQueryHandler(
    ICreatorChannelRepository channelRepository,
    ICreatorProfileLookup creatorProfileLookup,
    ICourseRepository courseRepository,
    IEnrollmentRepository enrollmentRepository
) : IRequestHandler<GetPublicChannelQuery, PublicChannelResponse?>
{
    public async Task<PublicChannelResponse?> Handle(GetPublicChannelQuery request, CancellationToken ct)
    {
        var channel = await channelRepository.GetByHandleAsync(request.Handle, ct);
        if (channel is null) return null;

        var profile = await creatorProfileLookup.GetProfileAsync(channel.CreatorId, ct);

        var publishedCourses = (await courseRepository.ListByInstructorAsync(channel.CreatorId, ct))
            .Where(c => c.Status == CourseStatus.Published)
            .OrderByDescending(c => c.PublishedAt)
            .ToList();

        var courses = new List<PublicChannelCourse>();
        foreach (var course in publishedCourses)
        {
            // Grátis é sempre liberado; pago exige o visitante estar logado E matriculado —
            // mesma checagem que a página do curso já faz, só reaproveitada aqui para o cadeado.
            var isUnlocked = course.IsFree
                || (request.ViewerUserId.HasValue
                    && await enrollmentRepository.IsEnrolledAsync(request.ViewerUserId.Value, course.Id, ct));

            courses.Add(new PublicChannelCourse(
                course.Id, course.Title, course.Slug.Value, course.ThumbnailUrl,
                course.Price.Amount, course.IsFree, isUnlocked));
        }

        return new PublicChannelResponse(
            channel.Id, channel.Handle.Value,
            profile?.DisplayName ?? "Criador Tuilow", profile?.AvatarUrl, profile?.Bio,
            channel.SocialLinks.Select(l => new PublicSocialLink(l.Platform, l.Url)).ToList(),
            courses);
    }
}
