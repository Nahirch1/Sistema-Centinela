namespace SentinelCase.Agent;

public sealed record TelemetryEvent(
    int EventType,
    string SourceIdentifier,
    string Message,
    DateTimeOffset OccurredAt);
