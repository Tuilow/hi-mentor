using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using DogMaster.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DogMaster.Infrastructure.Services.Streaming;

public sealed class CloudflareStreamService(
    HttpClient httpClient,
    IConfiguration configuration
) : IStreamingService
{
    private readonly string _accountId = configuration["Cloudflare:AccountId"]
        ?? throw new InvalidOperationException("Cloudflare:AccountId não configurado.");
    private readonly string _streamSigningKey = configuration["Cloudflare:StreamSigningKey"] ?? "";

    public async Task<DirectUploadResult> GetDirectUploadUrlAsync(CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync(
            $"/client/v4/accounts/{_accountId}/stream/direct_upload",
            new StringContent(
                JsonSerializer.Serialize(new { maxDurationSeconds = 3600, requireSignedURLs = true }),
                Encoding.UTF8, "application/json"), ct);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(ct);
        var doc    = JsonDocument.Parse(content);
        var result = doc.RootElement.GetProperty("result");

        var uid       = result.GetProperty("uid").GetString()!;
        var uploadUrl = result.GetProperty("uploadURL").GetString()!;

        return new DirectUploadResult(uid, uploadUrl);
    }

    public Task<string> GetSignedPlaybackUrlAsync(string cloudflareVideoId,
        int expirationMinutes = 60, CancellationToken ct = default)
    {
        // Gera JWT assinado para Cloudflare Stream
        if (string.IsNullOrEmpty(_streamSigningKey))
        {
            // Modo desenvolvimento: URL sem assinatura
            return Task.FromResult($"https://iframe.cloudflarestream.com/{cloudflareVideoId}");
        }

        var key = new SymmetricSecurityKey(Convert.FromBase64String(_streamSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: [
                new Claim("sub", cloudflareVideoId),
                new Claim("exp", DateTimeOffset.UtcNow.AddMinutes(expirationMinutes).ToUnixTimeSeconds().ToString()),
                new Claim("nbf", DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds().ToString()),
                new Claim("aud", _accountId),
            ],
            signingCredentials: credentials);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return Task.FromResult($"https://iframe.cloudflarestream.com/{cloudflareVideoId}?token={jwt}");
    }

    public async Task DeleteVideoAsync(string cloudflareVideoId, CancellationToken ct = default)
    {
        await httpClient.DeleteAsync(
            $"/client/v4/accounts/{_accountId}/stream/{cloudflareVideoId}", ct);
    }
}
