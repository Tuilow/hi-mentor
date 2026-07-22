using MediatR;

namespace Tuilow.Streaming.Application.Commands.DeleteVideo;

/// <summary>
/// Remove um vídeo enviado/importado que ainda não foi vinculado a nenhuma aula — usado tanto
/// para "descartar" um vídeo com Status=Error (ex.: falha no download do YouTube) quanto pra
/// limpar um vídeo importado por engano no passo 2 do assistente.
/// </summary>
public sealed record DeleteVideoCommand(Guid VideoId, Guid InstructorId) : IRequest;
