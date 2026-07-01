using DogMaster.Domain.Common.Interfaces;
using DogMaster.Domain.Contexts.Streaming.Interfaces;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace DogMaster.API.Controllers;

/// <summary>
/// Endpoint TUS mock para desenvolvimento sem Cloudflare Stream.
/// Só existe quando Cloudflare:MockMode = true.
/// Aceita qualquer upload TUS e marca o vídeo como pronto automaticamente.
/// </summary>
[ApiController]
[Route("api/v1/mock/tus")]
public sealed class MockTusController(
    IVideoRepository videoRepository,
    IUnitOfWork uow,
    IConfiguration configuration,
    ILogger<MockTusController> logger
) : ControllerBase
{
    private bool IsMockEnabled =>
        configuration.GetValue<bool>("Cloudflare:MockMode");

    // OPTIONS — TUS discovery (não é preflight CORS — é descoberta do protocolo TUS)
    [HttpOptions("{uid}")]
    public IActionResult Options(string uid)
    {
        if (!IsMockEnabled) return NotFound();
        AddTusHeaders();
        return Ok();
    }

    // HEAD — retorna offset atual; Upload-Defer-Length=1 indica tamanho ainda desconhecido
    [HttpHead("{uid}")]
    public IActionResult Head(string uid)
    {
        if (!IsMockEnabled) return NotFound();
        AddTusHeaders();
        Response.Headers["Upload-Offset"]       = "0";
        Response.Headers["Upload-Defer-Length"] = "1"; // tamanho será enviado no primeiro PATCH
        Response.Headers["Cache-Control"]       = "no-store";
        return Ok();
    }

    // PATCH — recebe chunk do upload, descarta o body e responde com offset
    [HttpPatch("{uid}")]
    [Consumes("application/offset+octet-stream")]
    [DisableRequestSizeLimit]                    // remove limite de 30 MB do Kestrel
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<IActionResult> Patch(string uid, CancellationToken ct)
    {
        if (!IsMockEnabled) return NotFound();

        // Desabilita buffering para não estourar memória com vídeos grandes
        var bufferingFeature = HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (bufferingFeature is not null) bufferingFeature.MaxRequestBodySize = null;

        // Lê (e descarta) o body para simular recebimento do chunk
        await Request.Body.CopyToAsync(Stream.Null, ct);

        var contentLength = Request.ContentLength ?? 0;
        var previousOffset = long.TryParse(
            Request.Headers["Upload-Offset"].FirstOrDefault(), out var off) ? off : 0;
        var newOffset = previousOffset + contentLength;

        AddTusHeaders();
        Response.Headers["Upload-Offset"] = newOffset.ToString();

        // Quando Upload-Length == novo offset, o upload terminou → marca vídeo como pronto
        if (Request.Headers.TryGetValue("Upload-Length", out var uploadLengthHeader) &&
            long.TryParse(uploadLengthHeader.FirstOrDefault(), out var uploadLength) &&
            newOffset >= uploadLength)
        {
            await MarkVideoReadyAsync(uid, ct);
        }

        return NoContent();
    }

    private async Task MarkVideoReadyAsync(string uid, CancellationToken ct)
    {
        try
        {
            var video = await videoRepository.GetByCloudflareIdAsync(uid, ct);
            if (video is null)
            {
                logger.LogWarning("Mock TUS: vídeo uid={Uid} não encontrado no banco.", uid);
                return;
            }

            video.MarkReady(durationSeconds: 60, thumbnailUrl: null);
            videoRepository.Update(video);
            await uow.SaveChangesAsync(ct);
            logger.LogInformation("Mock TUS: vídeo {Uid} marcado como pronto.", uid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Mock TUS: erro ao marcar vídeo {Uid} como pronto.", uid);
        }
    }

    private void AddTusHeaders()
    {
        Response.Headers["Tus-Resumable"] = "1.0.0";
        Response.Headers["Tus-Version"]   = "1.0.0";
        Response.Headers["Tus-Max-Size"]  = (5L * 1024 * 1024 * 1024).ToString(); // 5 GB
    }
}
