using SentinelCase.Domain.Entities;

namespace SentinelCase.Application.Common.Interfaces;

public interface ISecurityEventRepository
{
    Task AddAsync(
        SecurityEvent securityEvent,
        CancellationToken cancellationToken = default);

    Task<int> CountByAssetAndTypeSinceAsync(
        Guid monitoredAssetId,
        SentinelCase.Domain.Enums.SecurityEventType eventType,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);
}
