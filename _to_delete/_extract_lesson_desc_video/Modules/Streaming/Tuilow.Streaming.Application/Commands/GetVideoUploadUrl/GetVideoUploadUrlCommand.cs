using MediatR;

namespace Tuilow.Streaming.Application.Commands.GetVideoUploadUrl;

/// <summary>
/// CourseId/InstructorId: passo 2 do assistente já roda com o produto criado (passo 1 sempre
/// acontece antes) — gravar o vínculo aqui permite recarregar "meus vídeos deste produto ainda
/// não vinculados a uma aula" se o criador sair e voltar ao assistente antes do passo 3.
/// </summary>
public sealed record GetVideoUploadUrlCommand(Guid CourseId, Guid InstructorId, string? Title = null) : IRequest<VideoUploadUrlResponse>;

public sealed record VideoUploadUrlResponse(
    Guid VideoId,           // ID no nosso banco — salvar para vincular à aula depois
    string CloudflareVideoId, // uid do Cloudflare — usado pelo player
    string UploadUrl        // URL TUS — o browser envia o arquivo diretamente aqui
);
