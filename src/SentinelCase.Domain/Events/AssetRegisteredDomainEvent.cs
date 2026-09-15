using SentinelCase.Domain.Common;

namespace SentinelCase.Domain.Events;

public sealed record AssetRegisteredDomainEvent(
    Guid AssetId,
    string Name)
    : IDomainEvent;
