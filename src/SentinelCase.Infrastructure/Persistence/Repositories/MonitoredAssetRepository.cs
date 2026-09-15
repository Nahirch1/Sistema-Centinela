using Microsoft.EntityFrameworkCore;

using SentinelCase.Application.Common.Interfaces;
using SentinelCase.Domain.Entities;

namespace SentinelCase.Infrastructure.Persistence.Repositories;

internal sealed class MonitoredAssetRepository
    : IMonitoredAssetRepository
{
    private readonly ApplicationDbContext _dbContext;

    public MonitoredAssetRepository(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        MonitoredAsset asset,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.MonitoredAssets.AddAsync(
            asset,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<MonitoredAsset?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.MonitoredAssets
            .AsNoTracking()
            .SingleOrDefaultAsync(
                asset => asset.Id == id,
                cancellationToken);
    }

    public Task<MonitoredAsset?> GetByApiKeyHashAsync(
        string apiKeyHash,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.MonitoredAssets
            .AsNoTracking()
            .SingleOrDefaultAsync(
                asset => asset.ApiKeyHash == apiKeyHash,
                cancellationToken);
    }

    public Task<bool> ExistsWithNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.MonitoredAssets
            .AsNoTracking()
            .AnyAsync(
                asset => asset.Name == name,
                cancellationToken);
    }

    public async Task UpdateAsync(
        MonitoredAsset asset,
        CancellationToken cancellationToken = default)
    {
        _dbContext.MonitoredAssets.Update(asset);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
