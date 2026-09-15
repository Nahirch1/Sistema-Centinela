using MediatR;

using SentinelCase.Domain.Enums;

namespace SentinelCase.Application.Features.Telemetry.Commands.IngestSecurityEvent;

/// <summary>
/// MonitoredAssetId is resolved by the API key authentication
/// handler, not supplied by the caller - the agent authenticates
/// as itself, it cannot report events on behalf of another asset.
/// </summary>
public sealed record IngestSecurityEventCommand(
    Guid MonitoredAssetId,
    SecurityEventType EventType,
    string SourceIdentifier,
    string Message,
    DateTimeOffset OccurredAt) : IRequest<IngestSecurityEventResult>;
