using SentinelCase.Application.Common.Interfaces;
using SentinelCase.Application.Common.Models;
using SentinelCase.Domain.Entities;
using SentinelCase.Domain.Enums;

namespace SentinelCase.Application.Features.Telemetry.DetectionRules;

/// <summary>
/// Flags a burst of failed login attempts reported by the same
/// MonitoredAsset within a short window. This is a per-asset signal,
/// not per source IP - a coarse first rule, easy to make more
/// specific once real telemetry patterns are observed.
/// </summary>
public sealed class RepeatedFailedLoginDetectionRule
    : IIncidentDetectionRule
{
    private const int FailedAttemptThreshold = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(5);

    private readonly ISecurityEventRepository _eventRepository;

    public RepeatedFailedLoginDetectionRule(
        ISecurityEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<DetectedIncident?> EvaluateAsync(
        SecurityEvent securityEvent,
        CancellationToken cancellationToken)
    {
        if (securityEvent.EventType != SecurityEventType.FailedLoginAttempt)
        {
            return null;
        }

        var since = securityEvent.OccurredAt - Window;

        var count = await _eventRepository.CountByAssetAndTypeSinceAsync(
            securityEvent.MonitoredAssetId,
            SecurityEventType.FailedLoginAttempt,
            since,
            cancellationToken);

        if (count < FailedAttemptThreshold)
        {
            return null;
        }

        var windowBucket = securityEvent.OccurredAt
            .ToUnixTimeSeconds() / (long)Window.TotalSeconds;

        return new DetectedIncident(
            $"Repeated failed login attempts - asset {securityEvent.MonitoredAssetId} - window {windowBucket}",
            $"{count} failed login attempts were reported by asset " +
            $"{securityEvent.MonitoredAssetId} within a {Window.TotalMinutes:F0}-minute window. " +
            $"Most recent source: {securityEvent.SourceIdentifier}.",
            IncidentSeverity.High);
    }
}
