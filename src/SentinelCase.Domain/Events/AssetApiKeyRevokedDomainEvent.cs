using SentinelCase.Domain.Common;

namespace SentinelCase.Domain.Events;

public sealed record AssetApiKeyRevokedDomainEvent(
    Guid AssetId)
    : IDomainEvent;
