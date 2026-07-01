using Tuilow.Domain.Common.Interfaces;
using Tuilow.Domain.Contexts.Streaming.Interfaces;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Tuilow.API.Controllers;

/// <summary>
/// Endpoint TUS mock para desenvolvimento sem Cloudflare Stream.
/// Salva o vídeo em disco (mock-videos/{uid}) e serve via GET /api/v1/mock/videos/{uid}.
/// Só ativo quando Cloudflare:MockMode = true.
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

    private static string MockVideosDir =>
        Path.Combine(Directory.GetCurrentDirectory(), "mock-videos");

    private static string VideoPath(string uid) =>
        Path.Combine(MockVideosDir, uid);

    private static string MetaPath(string uid) =>
        Path.Combine(MockVideosDir, uid + ".meta");

    // ─── OPTIONS — TUS discovery ──────────────────────────────────────────────
    [HttpOptions("{uid}")]
    public IActionResult Options(string uid)
    {
        if (!IsMockEnabled) return NotFound();
        AddTusHeaders();
        return Ok();
    }

    // ─── HEAD — offset atual; Upload-Defer-Length pois o tamanho vem no PATCH ──
    [HttpHead("{uid}")]
    public IActionResult Head(string uid)
    {
        if (!IsMockEnabled) return NotFound();
        AddTusHeaders();

        // Se já há um arquivo parcial, retorna o offset real
        var path = VideoPath(uid);
        var offset = System.IO.File.Exists(path) ? new FileInfo(path).Length : 0;

        Response.Headers["Upload-Offset"]       = offset.ToString();
        Response.Headers["Upload-Defer-Length"] = "1";
        Response.Headers["Cache-Control"]       = "no-store";
        return Ok();
    }

    // ─── PATCH — grava o chunk em disco ──────────────────────────────────────
    [HttpPatch("{uid}")]
    [Consumes("application/offset+octet-stream")]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<IActionResult> Patch(string uid, CancellationToken ct)
    {
        if (!IsMockEnabled) return NotFound();

        var bufferingFeature = HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (bufferingFeature is not null) bufferingFeature.MaxRequestBodySize = null;

        Directory.CreateDirectory(MockVideosDir);

        // Na primeira PATCH, salva o content-type do arquivo
        var previousOffset = long.TryParse(
            Request.Headers["Upload-Offset"].FirstOrDefault(), out var off) ? off : 0;

        if (previousOffset == 0)
            SaveMeta(uid, Request.Headers["Upload-Metadata"].FirstOrDefault() ?? "");

        // Grava o chunk no arquivo (append)
        await using (var fs = new FileStream(VideoPath(uid), FileMode.Append, FileAccess.Write, FileShare.None))
            await Request.Body.CopyToAsync(fs, ct);

        var contentLength = Request.ContentLength ?? new FileInfo(VideoPath(uid)).Length - previousOffset;
        var newOffset = previousOffset + contentLength;

        AddTusHeaders();
        Response.Headers["Upload-Offset"] = newOffset.ToString();

        // Upload completo quando offset == Upload-Length
        if (Request.Headers.TryGetValue("Upload-Length", out var uploadLengthHeader) &&
            long.TryParse(uploadLengthHeader.FirstOrDefault(), out var uploadLength) &&
            newOffset >= uploadLength)
        {
            await MarkVideoReadyAsync(uid, ct);
        }

        return NoContent();
    }

    // ─── GET — serve o vídeo salvo em disco ──────────────────────────────────
    [HttpGet("/api/v1/mock/videos/{uid}")]
    public IActionResult GetVideo(string uid)
    {
        if (!IsMockEnabled) return NotFound();

        var path = VideoPath(uid);
        if (!System.IO.File.Exists(path)) return NotFound();

        var contentType = ReadContentType(uid);
        return PhysicalFile(path, contentType, enableRangeProcessing: true);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task MarkVideoReadyAsync(string uid, CancellationToken ct)
    {
        try
        {
            var video = await videoRepository.GetByCloudflareIdAsync(uid, ct);
            if (video is null) { logger.LogWarning("Mock TUS: uid={Uid} não encontrado.", uid); return; }

            video.MarkReady(durationSeconds: 60, thumbnailUrl: null);
            videoRepository.Update(video);
            await uow.SaveChangesAsync(ct);
            logger.LogInformation("Mock TUS: vídeo {Uid} pronto.", uid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Mock TUS: erro ao marcar {Uid} como pronto.", uid);
        }
    }

    /// <summary>Salva content-type parseando o header Upload-Metadata do TUS.</summary>
    private static void SaveMeta(string uid, string tusMetadata)
    {
        // Formato: "key base64val,key2 base64val2"
        var contentType = "video/mp4"; // padrão
        foreach (var pair in tusMetadata.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Trim().Split(' ', 2);
            if (parts.Length == 2 && parts[0] == "filetype")
            {
                try { contentType = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1])); }
                catch { /* ignora base64 inválido */ }
                break;
            }
        }
        System.IO.File.WriteAllText(MetaPath(uid), contentType);
    }

    private static string ReadContentType(string uid)
    {
        var metaPath = MetaPath(uid);
        return System.IO.File.Exists(metaPath)
            ? System.IO.File.ReadAllText(metaPath).Trim()
            : "video/mp4";
    }

    private void AddTusHeaders()
    {
        Response.Headers["Tus-Resumable"] = "1.0.0";
        Response.Headers["Tus-Version"]   = "1.0.0";
        Response.Headers["Tus-Max-Size"]  = (5L * 1024 * 1024 * 1024).ToString();
    }
}
