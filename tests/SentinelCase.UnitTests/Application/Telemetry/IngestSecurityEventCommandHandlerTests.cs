using Microsoft.Extensions.Time.Testing;

using SentinelCase.Application.Common.Interfaces;
using SentinelCase.Application.Features.Telemetry.Commands.IngestSecurityEvent;
using SentinelCase.Domain.Entities;
using SentinelCase.Domain.Enums;
using SentinelCase.Domain.Exceptions;
using SentinelCase.UnitTests.TestDoubles;

namespace SentinelCase.UnitTests.Application.Telemetry;

public sealed class IngestSecurityEventCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_ShouldPersistEventAndUpdateHeartbeat()
    {
        var registeredAt = new DateTimeOffset(
            2026,
            9,
            15,
            12,
            0,
            0,
            TimeSpan.Zero);

        var receivedAt = registeredAt.AddMinutes(5);

        var timeProvider = new FakeTimeProvider(receivedAt);
        var assetRepository = new FakeMonitoredAssetRepository();
        var eventRepository = new FakeSecurityEventRepository();
        var incidentRepository = new FakeSecurityIncidentRepository();
        var historyRepository = new FakeIncidentHistoryRepository();
        var detectionRules = Array.Empty<IIncidentDetectionRule>();

        var asset = MonitoredAsset.Create(
            "web-server-01",
            "hashed-api-key-value",
            registeredAt);

        await assetRepository.AddAsync(asset);

        var handler = new IngestSecurityEventCommandHandler(
            assetRepository,
            eventRepository,
            incidentRepository,
            historyRepository,
            detectionRules,
            timeProvider);

        var command = new IngestSecurityEventCommand(
            asset.Id,
            SecurityEventType.FailedLoginAttempt,
            "198.51.100.23",
            "Failed login for user 'admin'.",
            registeredAt.AddMinutes(4));

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(receivedAt, result.ReceivedAt);

        var storedEvent = Assert.Single(eventRepository.Events);

        Assert.Equal(result.Id, storedEvent.Id);
        Assert.Equal(asset.Id, storedEvent.MonitoredAssetId);
        Assert.Equal(
            SecurityEventType.FailedLoginAttempt,
            storedEvent.EventType);

        Assert.Equal(receivedAt, asset.LastSeenAt);
    }

    [Fact]
    public async Task Handle_WithNonExistentAsset_ShouldThrowDomainException()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var assetRepository = new FakeMonitoredAssetRepository();
        var eventRepository = new FakeSecurityEventRepository();
        var incidentRepository = new FakeSecurityIncidentRepository();
        var historyRepository = new FakeIncidentHistoryRepository();
        var detectionRules = Array.Empty<IIncidentDetectionRule>();

        var handler = new IngestSecurityEventCommandHandler(
            assetRepository,
            eventRepository,
            incidentRepository,
            historyRepository,
            detectionRules,
            timeProvider);

        var command = new IngestSecurityEventCommand(
            Guid.NewGuid(),
            SecurityEventType.FailedLoginAttempt,
            "198.51.100.23",
            "Failed login.",
            DateTimeOffset.UtcNow);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Equal(
            "The monitored asset does not exist.",
            exception.Message);

        Assert.Empty(eventRepository.Events);
    }

    [Fact]
    public async Task Handle_WithRevokedAsset_ShouldThrowDomainException()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var assetRepository = new FakeMonitoredAssetRepository();
        var eventRepository = new FakeSecurityEventRepository();
        var incidentRepository = new FakeSecurityIncidentRepository();
        var historyRepository = new FakeIncidentHistoryRepository();
        var detectionRules = Array.Empty<IIncidentDetectionRule>();

        var asset = MonitoredAsset.Create(
            "web-server-01",
            "hashed-api-key-value",
            DateTimeOffset.UtcNow);

        asset.RevokeApiKey();

        await assetRepository.AddAsync(asset);

        var handler = new IngestSecurityEventCommandHandler(
            assetRepository,
            eventRepository,
            incidentRepository,
            historyRepository,
            detectionRules,
            timeProvider);

        var command = new IngestSecurityEventCommand(
            asset.Id,
            SecurityEventType.FailedLoginAttempt,
            "198.51.100.23",
            "Failed login.",
            DateTimeOffset.UtcNow);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Equal(
            "A revoked asset cannot report telemetry.",
            exception.Message);

        Assert.Empty(eventRepository.Events);
    }

    [Fact]
    public async Task Handle_WithFiveFailedLoginsInWindow_ShouldCreateIncidentAutomatically()
    {
        var baseTime = new DateTimeOffset(
            2026,
            9,
            15,
            12,
            0,
            0,
            TimeSpan.Zero);

        var timeProvider = new FakeTimeProvider(baseTime);
        var assetRepository = new FakeMonitoredAssetRepository();
        var eventRepository = new FakeSecurityEventRepository();
        var incidentRepository = new FakeSecurityIncidentRepository();
        var historyRepository = new FakeIncidentHistoryRepository();

        var asset = MonitoredAsset.Create(
            "web-server-01",
            "hashed-api-key-value",
            baseTime);

        await assetRepository.AddAsync(asset);

        var rule = new SentinelCase.Application.Features.Telemetry
            .DetectionRules.RepeatedFailedLoginDetectionRule(
                eventRepository);

        var handler = new IngestSecurityEventCommandHandler(
            assetRepository,
            eventRepository,
            incidentRepository,
            historyRepository,
            [rule],
            timeProvider);

        for (var i = 0; i < 5; i++)
        {
            timeProvider.SetUtcNow(baseTime.AddSeconds(i + 1));

            var command = new IngestSecurityEventCommand(
                asset.Id,
                SecurityEventType.FailedLoginAttempt,
                "198.51.100.23",
                "Failed login for user 'admin'.",
                baseTime.AddSeconds(i));

            await handler.Handle(command, CancellationToken.None);
        }

        var incident = Assert.Single(incidentRepository.Incidents);

        Assert.Contains("Repeated failed login attempts", incident.Title);

        var historyEntry = Assert.Single(historyRepository.Entries);

        Assert.Equal("detection-engine", historyEntry.PerformedBy);
    }
}
