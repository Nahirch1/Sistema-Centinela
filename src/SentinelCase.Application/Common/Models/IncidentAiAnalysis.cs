using SentinelCase.Domain.Enums;

namespace SentinelCase.Application.Common.Models;

public sealed record IncidentAiAnalysis(
    string Summary,
    IncidentSeverity SeverityAssessment,
    string Category,
    IReadOnlyCollection<string> Indicators,
    IReadOnlyCollection<string> RecommendedActions,
    double Confidence,
    string ModelId,
    string PromptVersion);
