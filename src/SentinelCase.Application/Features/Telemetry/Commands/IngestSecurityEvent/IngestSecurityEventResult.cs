namespace SentinelCase.Application.Features.Telemetry.Commands.IngestSecurityEvent;

public sealed record IngestSecurityEventResult(
    Guid Id,
    DateTimeOffset ReceivedAt);
