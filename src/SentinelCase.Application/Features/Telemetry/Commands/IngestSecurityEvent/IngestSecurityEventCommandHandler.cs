using MediatR;

using SentinelCase.Application.Common.Interfaces;
using SentinelCase.Domain.Entities;
using SentinelCase.Domain.Exceptions;

namespace SentinelCase.Application.Features.Telemetry.Commands.IngestSecurityEvent;

public sealed class IngestSecurityEventCommandHandler
    : IRequestHandler<IngestSecurityEventCommand, IngestSecurityEventResult>
{
    private readonly IMonitoredAssetRepository _assetRepository;
    private readonly ISecurityEventRepository _eventRepository;
    private readonly TimeProvider _timeProvider;

    public IngestSecurityEventCommandHandler(
        IMonitoredAssetRepository assetRepository,
        ISecurityEventRepository eventRepository,
        TimeProvider timeProvider)
    {
        _assetRepository = assetRepository;
        _eventRepository = eventRepository;
        _timeProvider = timeProvider;
    }

    public async Task<IngestSecurityEventResult> Handle(
        IngestSecurityEventCommand request,
        CancellationToken cancellationToken)
    {
        var asset = await _assetRepository.GetByIdAsync(
            request.MonitoredAssetId,
            cancellationToken);

        if (asset is null)
        {
            throw new DomainException(
                "The monitored asset does not exist.");
        }

        var receivedAt = _timeProvider.GetUtcNow();

        // Validate the asset can report telemetry BEFORE creating or
        // persisting anything - a revoked asset must not have a
        // SecurityEvent left behind by a rejected request.
        asset.RecordHeartbeat(receivedAt);

        var securityEvent = SecurityEvent.Create(
            request.MonitoredAssetId,
            request.EventType,
            request.SourceIdentifier,
            request.Message,
            request.OccurredAt,
            receivedAt);

        await _eventRepository.AddAsync(
            securityEvent,
            cancellationToken);

        await _assetRepository.UpdateAsync(
            asset,
            cancellationToken);

        return new IngestSecurityEventResult(
            securityEvent.Id,
            securityEvent.ReceivedAt);
    }
}
