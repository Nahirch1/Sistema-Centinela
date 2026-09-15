using SentinelCase.Domain.Common;
using SentinelCase.Domain.Enums;

namespace SentinelCase.Domain.Events;

public sealed record SecurityEventReceivedDomainEvent(
    Guid SecurityEventId,
    Guid MonitoredAssetId,
    SecurityEventType EventType)
    : IDomainEvent;
