using SentinelCase.Domain.Enums;

namespace SentinelCase.Application.Features.Incidents.Queries.AnalyzeIncidentWithAi;

public sealed record AnalyzeIncidentWithAiResult(
    Guid IncidentId,
    string Summary,
    IncidentSeverity SeverityAssessment,
    string Category,
    IReadOnlyCollection<string> Indicators,
    IReadOnlyCollection<string> RecommendedActions,
    double Confidence,
    string ModelId,
    string PromptVersion);
