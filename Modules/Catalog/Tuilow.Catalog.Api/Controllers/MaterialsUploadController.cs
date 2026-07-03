using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.Catalog.Api.Controllers;

/// <summary>
/// Upload de materiais de aula (PDF/DOCX/PPTX/ZIP/imagem/planilha) — passo 4 do assistente.
/// Implementação local em disco, mesma filosofia "mock real" do MockTusController (Streaming):
/// funciona de ponta a ponta hoje sem nenhuma credencial de nuvem; trocar por um provedor real
/// (S3/Azure Blob/Cloudflare R2) no futuro é só substituir este controller por um serviço que
/// implemente o mesmo contrato de resposta, sem mudar o restante do fluxo (AddLessonAttachmentCommand
/// só recebe uma FileUrl pronta — não sabe nem precisa saber onde ela está hospedada).
/// </summary>
[ApiController]
[Route("api/v1/materials")]
[Authorize(Roles = "Creator,Admin")]
public sealed class MaterialsUploadController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".zip",
        ".jpg", ".jpeg", ".png", ".gif", ".webp",
        ".xls", ".xlsx", ".csv"
    };

    private static string MaterialsDir =>
        Path.Combine(Directory.GetCurrentDirectory(), "mock-materials");

    // storedName é sempre gerado por nós como {Guid:N}{extensão} — qualquer coisa fora desse
    // formato é rejeitada antes de tocar o disco (evita path traversal via "..", separadores
    // de diretório, etc. no {storedName} vindo da rota).
    private static readonly Regex StoredNamePattern = new(@"^[a-f0-9]{32}\.[a-zA-Z0-9]{1,10}$", RegexOptions.Compiled);

    [HttpPost("upload")]
    [RequestFormLimits(MultipartBodyLengthLimit = 100 * 1024 * 1024)]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Nenhum arquivo enviado." });

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
            return BadRequest(new { message = $"Tipo de arquivo não suportado: {extension}" });

        Directory.CreateDirectory(MaterialsDir);

        var storedName = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(MaterialsDir, storedName);

        await using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            await file.CopyToAsync(stream, ct);

        var url = $"{Request.Scheme}://{Request.Host}/api/v1/materials/{storedName}";

        return Ok(new
        {
            url,
            fileName = file.FileName,
            fileType = extension.TrimStart('.'),
            fileSizeBytes = file.Length
        });
    }

    [HttpGet("{storedName}")]
    [AllowAnonymous]
    public IActionResult GetMaterial(string storedName)
    {
        // Valida o formato ANTES de combinar com o diretório — nunca confia em input de rota
        // para montar um caminho de arquivo (path traversal).
        if (!StoredNamePattern.IsMatch(storedName)) return NotFound();

        var path = Path.Combine(MaterialsDir, storedName);
        if (!System.IO.File.Exists(path)) return NotFound();

        var contentType = "application/octet-stream";
        return PhysicalFile(path, contentType, enableRangeProcessing: true);
    }
}
