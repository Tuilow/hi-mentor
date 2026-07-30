using System.Text.RegularExpressions;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.SharedKernel.Application.Interfaces;
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
public sealed class MaterialsUploadController(
    ICourseRepository courseRepository,
    IUserCourseAccessService courseAccessService,
    ICurrentUserService currentUser
) : ControllerBase
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
    [Authorize(Roles = "Creator,Admin")]
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

    /// <summary>
    /// Achado M8 da avaliação: antes este endpoint era [AllowAnonymous], protegido só pela GUID
    /// imprevisível do nome do arquivo (StoredNamePattern) — qualquer pessoa com o link baixava
    /// o material sem nunca ter comprado/se matriculado no curso (link vazado em print, cache de
    /// navegador, encaminhado por um aluno etc.). Agora exige autenticação e replica a mesma
    /// regra de acesso já centralizada para o player de vídeo (ver
    /// Streaming.Application.GetLessonPlayUrlQueryHandler): o próprio criador do curso sempre
    /// pode baixar, aula marcada como preview é liberada pra qualquer usuário logado, senão
    /// exige matrícula/compra/assinatura via IUserCourseAccessService (mesma checagem única da
    /// plataforma — não reimplementa a regra aqui).
    /// [Authorize] aqui é independente do [Authorize(Roles = "Creator,Admin")] do Upload acima —
    /// não há atributo de role no nível da classe, então cada ação carrega só a exigência que
    /// faz sentido pra ela.
    /// </summary>
    [HttpGet("{storedName}")]
    [Authorize]
    public async Task<IActionResult> GetMaterial(string storedName, CancellationToken ct)
    {
        // Valida o formato ANTES de combinar com o diretório — nunca confia em input de rota
        // para montar um caminho de arquivo (path traversal).
        if (!StoredNamePattern.IsMatch(storedName)) return NotFound();

        var path = Path.Combine(MaterialsDir, storedName);
        if (!System.IO.File.Exists(path)) return NotFound();

        var url = $"{Request.Scheme}://{Request.Host}/api/v1/materials/{storedName}";
        var access = await courseRepository.GetMaterialAccessInfoAsync(url, ct);

        if (access is not null)
        {
            var isOwner = currentUser.UserId.HasValue && currentUser.UserId.Value == access.InstructorId;

            if (!isOwner && !access.IsPreview)
            {
                var hasAccess = currentUser.UserId.HasValue
                    && await courseAccessService.HasAccessAsync(currentUser.UserId.Value, access.CourseId, ct);

                if (!hasAccess) return Forbid();
            }
        }
        // access is null: não foi possível resolver o anexo pela FileUrl (ex.: material
        // enviado mas ainda não anexado a nenhuma aula salva). Mantém o comportamento de servir
        // o arquivo nesse caso — mas o endpoint já não é mais anônimo, então exige login mesmo
        // assim, o que sozinho já fecha a maior parte da exposição do achado original.

        var contentType = "application/octet-stream";
        return PhysicalFile(path, contentType, enableRangeProcessing: true);
    }
}
