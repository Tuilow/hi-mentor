using Tuilow.SharedKernel.Application.Common;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.Enums;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Channel.Application.Interfaces;
using Tuilow.Channel.Domain.Interfaces;
using Tuilow.Sales.Domain.Interfaces;
using MediatR;

namespace Tuilow.Channel.Application.Queries.GetPublicChannel;

public sealed class GetPublicChannelQueryHandler(
    ICreatorChannelRepository channelRepository,
    ICreatorProfileLookup creatorProfileLookup,
    ICourseRepository courseRepository,
    ISubscriptionRepository subscriptionRepository,
    IUserCourseAccessService courseAccessService
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
            // Estado real de comercialização (Grátis/Pago/Assinatura) — nunca deriva "Grátis" só
            // de course.IsFree isoladamente (ver CourseCommercializationResolver): um curso no
            // modo "Assinatura" grava Course.Price = 0 por design, então precisa checar o Plan
            // (Sales) antes de decidir. Este é o mesmo bug já corrigido em /c/[slug], só que
            // reaparecendo aqui de forma independente — motivo pelo qual passou a ser resolvido
            // em um único lugar (SharedKernel), não mais recalculado por página.
            var hasActivePlan = (await subscriptionRepository.GetPlansByCourseAsync(course.Id, ct))
                .Any(p => p.IsActive);
            var commercializationState = CourseCommercializationResolver.Resolve(
                isPublished: true, course.IsFree, hasActivePlan);

            // Grátis é sempre exibido como liberado (mesmo comportamento de antes — matricular-se
            // em curso grátis continua um passo separado, ver /c/[slug]); pago/assinatura exige
            // o visitante estar logado E ter acesso de fato — não mais uma checagem própria
            // desta tela, e sim o único serviço de acesso da plataforma (ver
            // IUserCourseAccessService): "visualizar o curso na vitrine" ≠ "possuir acesso".
            var isUnlocked = commercializationState == CourseCommercializationState.Free
                || (request.ViewerUserId.HasValue
                    && await courseAccessService.HasAccessAsync(request.ViewerUserId.Value, course.Id, ct));

            courses.Add(new PublicChannelCourse(
                course.Id, course.Title, course.Slug.Value, course.ThumbnailUrl,
                course.Price.Amount, course.IsFree, isUnlocked, commercializationState.ToString()));
        }

        return new PublicChannelResponse(
            channel.Id, channel.Handle.Value,
            profile?.DisplayName ?? "Criador Tuilow", profile?.AvatarUrl, profile?.Bio,
            channel.SocialLinks.Select(l => new PublicSocialLink(l.Platform, l.Url)).ToList(),
            courses, channel.BannerUrl, channel.IntroVideoUrl);
    }
}
