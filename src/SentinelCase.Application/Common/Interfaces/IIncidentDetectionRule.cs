using SentinelCase.Application.Common.Models;
using SentinelCase.Domain.Entities;

namespace SentinelCase.Application.Common.Interfaces;

/// <summary>
/// A rule evaluated after a SecurityEvent is persisted. Returns a
/// DetectedIncident when the accumulated signal warrants creating a
/// SecurityIncident, or null otherwise. Rules only recommend/detect -
/// they never mutate an existing incident or take destructive action.
/// </summary>
public interface IIncidentDetectionRule
{
    Task<DetectedIncident?> EvaluateAsync(
        SecurityEvent securityEvent,
        CancellationToken cancellationToken);
}
