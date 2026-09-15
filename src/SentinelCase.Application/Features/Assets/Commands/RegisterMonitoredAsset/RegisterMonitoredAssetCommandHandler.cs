using System.Security.Cryptography;
using System.Text;

using MediatR;

using SentinelCase.Application.Common.Interfaces;
using SentinelCase.Domain.Entities;
using SentinelCase.Domain.Exceptions;

namespace SentinelCase.Application.Features.Assets.Commands.RegisterMonitoredAsset;

public sealed class RegisterMonitoredAssetCommandHandler
    : IRequestHandler<RegisterMonitoredAssetCommand, RegisterMonitoredAssetResult>
{
    private readonly IMonitoredAssetRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RegisterMonitoredAssetCommandHandler(
        IMonitoredAssetRepository repository,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<RegisterMonitoredAssetResult> Handle(
        RegisterMonitoredAssetCommand request,
        CancellationToken cancellationToken)
    {
        var nameAlreadyExists =
            await _repository.ExistsWithNameAsync(
                request.Name.Trim(),
                cancellationToken);

        if (nameAlreadyExists)
        {
            throw new DomainException(
                "An asset with the same name is already registered.");
        }

        var apiKey = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(64));

        var apiKeyHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(apiKey)));

        var registeredAt = _timeProvider.GetUtcNow();

        var asset = MonitoredAsset.Create(
            request.Name,
            apiKeyHash,
            registeredAt);

        await _repository.AddAsync(
            asset,
            cancellationToken);

        return new RegisterMonitoredAssetResult(
            asset.Id,
            asset.Name,
            apiKey,
            asset.RegisteredAt);
    }
}
