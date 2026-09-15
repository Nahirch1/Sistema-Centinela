namespace SentinelCase.Application.Features.Assets.Commands.RegisterMonitoredAsset;

/// <summary>
/// ApiKey is the raw secret and is only ever returned here, at
/// registration time. It is never stored or retrievable again -
/// only its hash lives in the database.
/// </summary>
public sealed record RegisterMonitoredAssetResult(
    Guid Id,
    string Name,
    string ApiKey,
    DateTimeOffset RegisteredAt);
