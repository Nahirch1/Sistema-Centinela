using SentinelCase.Domain.Entities;

namespace SentinelCase.Application.Common.Interfaces;

public interface IMonitoredAssetRepository
{
    Task AddAsync(
        MonitoredAsset asset,
        CancellationToken cancellationToken = default);

    Task<MonitoredAsset?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<MonitoredAsset?> GetByApiKeyHashAsync(
        string apiKeyHash,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsWithNameAsync(
        string name,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        MonitoredAsset asset,
        CancellationToken cancellationToken = default);
}
