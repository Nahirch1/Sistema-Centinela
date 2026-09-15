using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SentinelCase.Application.Common.Interfaces;
using SentinelCase.Domain.Enums;

namespace SentinelCase.Api.Common.Authentication;

/// <summary>
/// Authenticates a MonitoredAsset's agent via the X-Api-Key header.
/// This is machine-to-machine authentication, separate from the
/// human JWT scheme - an agent authenticates as itself, so the
/// resulting principal only carries the asset's own identity.
/// </summary>
public sealed class ApiKeyAuthenticationHandler
    : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IMonitoredAssetRepository _assetRepository;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IMonitoredAssetRepository assetRepository)
        : base(options, logger, encoder)
    {
        _assetRepository = assetRepository;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(
                ApiKeyAuthenticationOptions.HeaderName,
                out var providedApiKey)
            || string.IsNullOrWhiteSpace(providedApiKey))
        {
            return AuthenticateResult.NoResult();
        }

        var apiKeyHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(providedApiKey.ToString())));

        var asset = await _assetRepository.GetByApiKeyHashAsync(
            apiKeyHash,
            Context.RequestAborted);

        if (asset is null)
        {
            return AuthenticateResult.Fail("Invalid API key.");
        }

        if (asset.Status != AssetStatus.Active)
        {
            return AuthenticateResult.Fail("The asset's API key has been revoked.");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, asset.Id.ToString()),
            new Claim(ClaimTypes.Name, asset.Name)
        };

        var identity = new ClaimsIdentity(
            claims,
            ApiKeyAuthenticationOptions.SchemeName);

        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(
            principal,
            ApiKeyAuthenticationOptions.SchemeName);

        return AuthenticateResult.Success(ticket);
    }
}
