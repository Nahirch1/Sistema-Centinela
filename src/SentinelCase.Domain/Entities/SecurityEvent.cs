using SentinelCase.Domain.Common;
using SentinelCase.Domain.Enums;
using SentinelCase.Domain.Events;
using SentinelCase.Domain.Exceptions;

namespace SentinelCase.Domain.Entities;

/// <summary>
/// A raw telemetry event reported by a MonitoredAsset's agent.
/// This is DATA, not a decision - the detection rule engine (later
/// phase) decides whether it warrants a SecurityIncident.
/// </summary>
public sealed class SecurityEvent : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    private const int MaximumMessageLength = 2000;
    private const int MaximumSourceIdentifierLength = 200;

    private SecurityEvent()
    {
    }

    private SecurityEvent(
        Guid id,
        Guid monitoredAssetId,
        SecurityEventType eventType,
        string sourceIdentifier,
        string message,
        DateTimeOffset occurredAt,
        DateTimeOffset receivedAt)
    {
        Id = id;
        MonitoredAssetId = monitoredAssetId;
        EventType = eventType;
        SourceIdentifier = sourceIdentifier;
        Message = message;
        OccurredAt = occurredAt;
        ReceivedAt = receivedAt;
    }

    public Guid Id { get; private set; }

    public Guid MonitoredAssetId { get; private set; }

    public SecurityEventType EventType { get; private set; }

    public string SourceIdentifier { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private set; }

    public DateTimeOffset ReceivedAt { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents =>
        _domainEvents.AsReadOnly();

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public static SecurityEvent Create(
        Guid monitoredAssetId,
        SecurityEventType eventType,
        string sourceIdentifier,
        string message,
        DateTimeOffset occurredAt,
        DateTimeOffset receivedAt)
    {
        if (monitoredAssetId == Guid.Empty)
        {
            throw new DomainException(
                "The monitored asset id is required.");
        }

        if (!Enum.IsDefined(eventType))
        {
            throw new DomainException(
                "The security event type is invalid.");
        }

        ValidateSourceIdentifier(sourceIdentifier);
        ValidateMessage(message);

        if (occurredAt > receivedAt)
        {
            throw new DomainException(
                "The event occurrence date cannot be later than its reception date.");
        }

        var securityEvent = new SecurityEvent(
            Guid.NewGuid(),
            monitoredAssetId,
            eventType,
            sourceIdentifier.Trim(),
            message.Trim(),
            occurredAt,
            receivedAt);

        securityEvent._domainEvents.Add(
            new SecurityEventReceivedDomainEvent(
                securityEvent.Id,
                securityEvent.MonitoredAssetId,
                securityEvent.EventType));

        return securityEvent;
    }

    private static void ValidateSourceIdentifier(string sourceIdentifier)
    {
        if (string.IsNullOrWhiteSpace(sourceIdentifier))
        {
            throw new DomainException(
                "The event source identifier is required.");
        }

        if (sourceIdentifier.Trim().Length > MaximumSourceIdentifierLength)
        {
            throw new DomainException(
                $"The source identifier cannot exceed {MaximumSourceIdentifierLength} characters.");
        }
    }

    private static void ValidateMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new DomainException(
                "The event message is required.");
        }

        if (message.Trim().Length > MaximumMessageLength)
        {
            throw new DomainException(
                $"The event message cannot exceed {MaximumMessageLength} characters.");
        }
    }
}
