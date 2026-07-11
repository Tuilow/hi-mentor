using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.CreatorStudio.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Tuilow.CreatorStudio.Infrastructure.Services;

/// <summary>
/// Implementação "vazia" do IVideoEditingService — não processa vídeo nenhum, só deixa a porta
/// pronta para um provedor real (ffmpeg local, ou uma API de IA de vídeo) ser plugado depois
/// sem precisar tocar em nenhum outro código do módulo. Mesmo padrão de NoOpWhatsAppService.
/// </summary>
public sealed class NoOpVideoEditingService(ILogger<NoOpVideoEditingService> logger) : IVideoEditingService
{
    public Task<VideoEditingCapabilities> GetCapabilitiesAsync(CancellationToken ct = default) =>
        Task.FromResult(new VideoEditingCapabilities(
            IsAvailable: false,
            StatusMessage: "Edição automática e clipes para redes sociais chegam em breve — essa funcionalidade " +
                "ainda está em preparação nesta instalação."));

    public Task<VideoAutoEditResult> AutoEditAsync(Guid videoId, CancellationToken ct = default)
    {
        logger.LogInformation(
            "AutoEditAsync chamado para o vídeo {VideoId}, mas nenhum provedor real de edição está configurado.", videoId);
        throw new BusinessException("A edição automática ainda não está disponível nesta instalação.");
    }

    public Task<IReadOnlyList<SocialClipSuggestion>> GenerateSocialClipsAsync(Guid videoId, CancellationToken ct = default)
    {
        logger.LogInformation(
            "GenerateSocialClipsAsync chamado para o vídeo {VideoId}, mas nenhum provedor real de IA de vídeo está configurado.", videoId);
        throw new BusinessException("A geração de clipes para redes sociais ainda não está disponível nesta instalação.");
    }
}
