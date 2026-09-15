using SentinelCase.Domain.Enums;

namespace SentinelCase.Application.Common.Models;

public sealed record IncidentAiAnalysisInput(
    Guid IncidentId,
    string Title,
    string Description,
    IncidentSeverity Severity,
    IncidentStatus Status,
    DateTimeOffset DetectedAt,
    IReadOnlyCollection<string> NoteContents,
    IReadOnlyCollection<string> HistorySummaries);
