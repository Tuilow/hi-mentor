using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.Streaming.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Tuilow.Streaming.Infrastructure.Services;

public sealed class CloudflareStreamService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<CloudflareStreamService> logger
) : IStreamingService
{
    private readonly string _accountId =
        !string.IsNullOrWhiteSpace(configuration["Cloudflare:AccountId"])
            ? configuration["Cloudflare:AccountId"]!
            : throw new InvalidOperationException(
                "Cloudflare:AccountId não configurado. Preencha appsettings.json > Cloudflare:AccountId.");

    /// <summary>
    /// Achado C3 da avaliação: só faz sentido exigir requireSignedURLs=true no Cloudflare se
    /// tivermos, de fato, como assinar (StreamSigningKeyId + StreamSigningKeyPem configurados —
    /// ver GetSignedPlaybackUrlAsync). Sem isso, marcar o vídeo como "assinatura obrigatória"
    /// deixaria QUALQUER playback quebrado (nem público, nem assinado) — pior que o estado
    /// atual. Gerar o par (id, pem) é feito uma vez via API do Cloudflare Stream
    /// (POST /accounts/{account_id}/stream/keys) e colado no appsettings/variável de ambiente.
    /// </summary>
    private bool SigningConfigured =>
        !string.IsNullOrWhiteSpace(configuration["Cloudflare:StreamSigningKeyId"])
        && !string.IsNullOrWhiteSpace(configuration["Cloudflare:StreamSigningKeyPem"]);

    public async Task<DirectUploadResult> GetDirectUploadUrlAsync(CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new
        {
            maxDurationSeconds = 3600,
            requireSignedURLs  = SigningConfigured
        });

        var response = await httpClient.PostAsync(
            $"/client/v4/accounts/{_accountId}/stream/direct_upload",
            new StringContent(body, Encoding.UTF8, "application/json"), ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Cloudflare Stream API (direct_upload) retornou {Status}: {Body}",
                (int)response.StatusCode, errorBody);
            throw new ExternalServiceException(
                $"Cloudflare Stream API retornou {(int)response.StatusCode}.");
        }

        var content = await response.Content.ReadAsStringAsync(ct);
        var doc    = JsonDocument.Parse(content);
        var result = doc.RootElement.GetProperty("result");

        var uid       = result.GetProperty("uid").GetString()!;
        var uploadUrl = result.GetProperty("uploadURL").GetString()!;

        return new DirectUploadResult(uid, uploadUrl);
    }

    /// <summary>
    /// Achado C3 da avaliação (CRÍTICO): antes este método ignorava expirationMinutes e sempre
    /// devolvia a URL pública do manifesto HLS — qualquer pessoa com o link (vazado,
    /// compartilhado, capturado em rede) assistia ou baixava o conteúdo pago indefinidamente,
    /// mesmo sem estar logada, ao contrário do que o README do projeto promete. Agora gera um
    /// JWT RS256 real (assinado com a chave privada do Cloudflare Stream Signing Key, formato
    /// exigido pela API deles: header com "kid", payload com "sub"=uid do vídeo, "exp"/"nbf")
    /// usando só System.Security.Cryptography (nenhuma dependência nova) — o token substitui o
    /// uid na URL, e o Cloudflare só serve o manifesto se o JWT for válido e ainda não tiver
    /// expirado. Continua caindo para a URL pública SE StreamSigningKeyId/Pem não estiverem
    /// configurados (documentado desde sempre na interface), mas agora loga um aviso alto nesse
    /// caso — antes o fallback acontecia sempre, em silêncio, mesmo com a chave configurada.
    /// </summary>
    public Task<string> GetSignedPlaybackUrlAsync(
        string cloudflareVideoId,
        int expirationMinutes = 60,
        CancellationToken ct = default)
    {
        if (!SigningConfigured)
        {
            logger.LogWarning(
                "Achado C3 da auditoria: Cloudflare:StreamSigningKeyId/StreamSigningKeyPem não " +
                "configurados — servindo o vídeo {VideoId} com URL PÚBLICA, sem assinatura nem " +
                "expiração. Gere uma Stream Signing Key na Cloudflare e configure as duas chaves " +
                "para fechar esta exposição.", cloudflareVideoId);

            return Task.FromResult(
                $"https://videodelivery.net/{cloudflareVideoId}/manifest/video.m3u8");
        }

        var keyId = configuration["Cloudflare:StreamSigningKeyId"]!;
        var pem   = configuration["Cloudflare:StreamSigningKeyPem"]!;

        var now = DateTimeOffset.UtcNow;
        var header  = JsonSerializer.Serialize(new { alg = "RS256", kid = keyId });
        // nbf com 2 minutos de folga pra absorver clock skew entre este servidor e o Cloudflare.
        var payload = JsonSerializer.Serialize(new
        {
            sub = cloudflareVideoId,
            kid = keyId,
            exp = now.AddMinutes(expirationMinutes).ToUnixTimeSeconds(),
            nbf = now.AddMinutes(-2).ToUnixTimeSeconds()
        });

        var signingInput = $"{Base64UrlEncode(Encoding.UTF8.GetBytes(header))}." +
                            $"{Base64UrlEncode(Encoding.UTF8.GetBytes(payload))}";

        using var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        var signature = rsa.SignData(
            Encoding.UTF8.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var token = $"{signingInput}.{Base64UrlEncode(signature)}";

        return Task.FromResult($"https://videodelivery.net/{token}/manifest/video.m3u8");
    }

    public async Task DeleteVideoAsync(string cloudflareVideoId, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync(
            $"/client/v4/accounts/{_accountId}/stream/{cloudflareVideoId}", ct);

        // 404 = vídeo já foi removido; ignora
        if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
            response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Upload direto (não-TUS) — usado pelo YouTubeDownloadWorker depois de baixar o vídeo com
    /// yt-dlp: o arquivo inteiro já está em disco, então um POST multipart simples é mais direto
    /// que simular um cliente TUS no servidor. Mesmo endpoint aceita tanto "uid" (upload de
    /// arquivo) quanto o direct_upload (TUS) usado no fluxo do navegador.
    /// </summary>
    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("video/mp4");
        content.Add(streamContent, "file", fileName);

        var response = await httpClient.PostAsync(
            $"/client/v4/accounts/{_accountId}/stream", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Cloudflare Stream API (upload) retornou {Status}: {Body}",
                (int)response.StatusCode, errorBody);
            throw new ExternalServiceException(
                $"Cloudflare Stream API (upload) retornou {(int)response.StatusCode}.");
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseBody);
        var uid = doc.RootElement.GetProperty("result").GetProperty("uid").GetString()!;

        // Achado C3: este caminho de upload não passa pelo body de direct_upload acima (que já
        // define requireSignedURLs na criação) — precisa de uma chamada separada de "editar
        // vídeo" pra ligar a mesma proteção. Best-effort: não derruba o upload (que já
        // terminou com sucesso) se essa segunda chamada falhar — só loga, pra não deixar um
        // vídeo importado do YouTube travado em "erro" por causa de uma etapa secundária.
        if (SigningConfigured)
        {
            try
            {
                var updateBody = JsonSerializer.Serialize(new { requireSignedURLs = true });
                var updateResponse = await httpClient.PostAsync(
                    $"/client/v4/accounts/{_accountId}/stream/{uid}",
                    new StringContent(updateBody, Encoding.UTF8, "application/json"), ct);

                if (!updateResponse.IsSuccessStatusCode)
                {
                    var errorBody = await updateResponse.Content.ReadAsStringAsync(ct);
                    logger.LogWarning(
                        "Não foi possível ativar requireSignedURLs para o vídeo {Uid} [{Status}]: {Body}",
                        uid, (int)updateResponse.StatusCode, errorBody);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Falha ao ativar requireSignedURLs para o vídeo {Uid}.", uid);
            }
        }

        return uid;
    }

    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
