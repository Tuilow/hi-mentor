using MediatR;

namespace Tuilow.Streaming.Application.Commands.GetVideoUploadUrl;

public sealed record GetVideoUploadUrlCommand : IRequest<VideoUploadUrlResponse>;

public sealed record VideoUploadUrlResponse(
    Guid VideoId,           // ID no nosso banco — salvar para vincular à aula depois
    string CloudflareVideoId, // uid do Cloudflare — usado pelo player
    string UploadUrl        // URL TUS — o browser envia o arquivo diretamente aqui
);
