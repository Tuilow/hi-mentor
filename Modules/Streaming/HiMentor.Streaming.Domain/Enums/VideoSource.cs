namespace HiMentor.Streaming.Domain.Enums;

/// <summary>
/// Origem do vídeo. Upload = enviado via TUS pro Cloudflare Stream (fluxo original, já
/// existente). Os demais valores vêm do passo 2 do assistente ("Conteúdo") — importação por
/// URL, evitando reenviar arquivo e reduzindo custo de armazenamento (estratégia explícita do
/// wizard: preferir sempre importar em vez de subir localmente).
/// </summary>
public enum VideoSource { Upload, YouTube, Vimeo, CloudflareStream, GoogleDrive, Dropbox, OneDrive }
