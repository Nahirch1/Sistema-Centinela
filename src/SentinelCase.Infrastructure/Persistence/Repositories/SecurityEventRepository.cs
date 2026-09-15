using Microsoft.EntityFrameworkCore;

using SentinelCase.Application.Common.Interfaces;
using SentinelCase.Domain.Entities;
using SentinelCase.Domain.Enums;

namespace SentinelCase.Infrastructure.Persistence.Repositories;

internal sealed class SecurityEventRepository
    : ISecurityEventRepository
{
    private readonly ApplicationDbContext _dbContext;

    public SecurityEventRepository(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        SecurityEvent securityEvent,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SecurityEvents.AddAsync(
            securityEvent,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<int> CountByAssetAndTypeSinceAsync(
        Guid monitoredAssetId,
        SecurityEventType eventType,
        DateTimeOffset since,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SecurityEvents
            .AsNoTracking()
            .CountAsync(
                securityEvent =>
                    securityEvent.MonitoredAssetId == monitoredAssetId &&
                    securityEvent.EventType == eventType &&
                    securityEvent.OccurredAt >= since,
                cancellationToken);
    }
}
