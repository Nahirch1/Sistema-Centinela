using SentinelCase.Domain.Enums;

namespace SentinelCase.Application.Common.Models;

public sealed record DetectedIncident(
    string Title,
    string Description,
    IncidentSeverity Severity);
