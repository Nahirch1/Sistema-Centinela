using MediatR;

using SentinelCase.Application.Common.Interfaces;
using SentinelCase.Domain.Entities;
using SentinelCase.Domain.Enums;
using SentinelCase.Domain.Exceptions;

namespace SentinelCase.Application.Features.Telemetry.Commands.IngestSecurityEvent;

public sealed class IngestSecurityEventCommandHandler
    : IRequestHandler<IngestSecurityEventCommand, IngestSecurityEventResult>
{
    private readonly IMonitoredAssetRepository _assetRepository;
    private readonly ISecurityEventRepository _eventRepository;
    private readonly ISecurityIncidentRepository _incidentRepository;
    private readonly IIncidentHistoryRepository _historyRepository;
    private readonly IEnumerable<IIncidentDetectionRule> _detectionRules;
    private readonly TimeProvider _timeProvider;

    public IngestSecurityEventCommandHandler(
        IMonitoredAssetRepository assetRepository,
        ISecurityEventRepository eventRepository,
        ISecurityIncidentRepository incidentRepository,
        IIncidentHistoryRepository historyRepository,
        IEnumerable<IIncidentDetectionRule> detectionRules,
        TimeProvider timeProvider)
    {
        _assetRepository = assetRepository;
        _eventRepository = eventRepository;
        _incidentRepository = incidentRepository;
        _historyRepository = historyRepository;
        _detectionRules = detectionRules;
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

        await EvaluateDetectionRulesAsync(
            securityEvent,
            receivedAt,
            cancellationToken);

        return new IngestSecurityEventResult(
            securityEvent.Id,
            securityEvent.ReceivedAt);
    }

    private async Task EvaluateDetectionRulesAsync(
        SecurityEvent securityEvent,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        foreach (var rule in _detectionRules)
        {
            var detected = await rule.EvaluateAsync(
                securityEvent,
                cancellationToken);

            if (detected is null)
            {
                continue;
            }

            var alreadyExists =
                await _incidentRepository.ExistsWithTitleAsync(
                    detected.Title,
                    cancellationToken);

            if (alreadyExists)
            {
                continue;
            }

            var incident = SecurityIncident.Create(
                detected.Title,
                detected.Description,
                detected.Severity,
                securityEvent.OccurredAt,
                receivedAt);

            await _incidentRepository.AddAsync(
                incident,
                cancellationToken);

            var historyEntry = IncidentHistoryEntry.Create(
                incident.Id,
                IncidentHistoryEventType.Created,
                "The incident was created automatically by the detection engine.",
                previousValue: null,
                newValue: incident.Status.ToString(),
                performedBy: "detection-engine",
                receivedAt);

            await _historyRepository.AddAsync(
                historyEntry,
                cancellationToken);
        }
    }
}
