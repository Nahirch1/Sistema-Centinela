using SentinelCase.Application.Common.Interfaces;
using SentinelCase.Domain.Entities;
using SentinelCase.Domain.Enums;

namespace SentinelCase.UnitTests.TestDoubles;

internal sealed class FakeSecurityEventRepository
    : ISecurityEventRepository
{
    private readonly List<SecurityEvent> _events = [];

    public IReadOnlyCollection<SecurityEvent> Events => _events;

    public Task AddAsync(
        SecurityEvent securityEvent,
        CancellationToken cancellationToken = default)
    {
        _events.Add(securityEvent);
        return Task.CompletedTask;
    }

    public Task<int> CountByAssetAndTypeSinceAsync(
        Guid monitoredAssetId,
        SecurityEventType eventType,
        DateTimeOffset since,
        CancellationToken cancellationToken = default)
    {
        var count = _events.Count(
            securityEvent =>
                securityEvent.MonitoredAssetId == monitoredAssetId &&
                securityEvent.EventType == eventType &&
                securityEvent.OccurredAt >= since);

        return Task.FromResult(count);
    }
}
