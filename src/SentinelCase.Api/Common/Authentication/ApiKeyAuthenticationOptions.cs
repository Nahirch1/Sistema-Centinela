using Microsoft.AspNetCore.Authentication;

namespace SentinelCase.Api.Common.Authentication;

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "ApiKey";

    public const string HeaderName = "X-Api-Key";
}
