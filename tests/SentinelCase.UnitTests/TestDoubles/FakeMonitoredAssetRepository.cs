using SentinelCase.Application.Common.Interfaces;
using SentinelCase.Domain.Entities;

namespace SentinelCase.UnitTests.TestDoubles;

internal sealed class FakeMonitoredAssetRepository
    : IMonitoredAssetRepository
{
    private readonly List<MonitoredAsset> _assets = [];

    public IReadOnlyCollection<MonitoredAsset> Assets => _assets;

    public Task AddAsync(
        MonitoredAsset asset,
        CancellationToken cancellationToken = default)
    {
        _assets.Add(asset);
        return Task.CompletedTask;
    }

    public Task<MonitoredAsset?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var asset = _assets.SingleOrDefault(item => item.Id == id);
        return Task.FromResult(asset);
    }

    public Task<MonitoredAsset?> GetByApiKeyHashAsync(
        string apiKeyHash,
        CancellationToken cancellationToken = default)
    {
        var asset = _assets.SingleOrDefault(
            item => item.ApiKeyHash == apiKeyHash);

        return Task.FromResult(asset);
    }

    public Task<bool> ExistsWithNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var exists = _assets.Any(
            asset => string.Equals(
                asset.Name,
                name,
                StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(exists);
    }

    public Task UpdateAsync(
        MonitoredAsset asset,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
