using System.Text.Json;
using System.Text.Json.Serialization;

using SentinelCase.Application.Common.Models;
using SentinelCase.Domain.Enums;

namespace SentinelCase.AI.Parsing;

public static class IncidentAiAnalysisJsonParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IncidentAiAnalysis Parse(
        string rawJson,
        string modelId,
        string promptVersion)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            throw new AiAnalysisParsingException(
                "The model returned an empty response.");
        }

        RawAnalysis? raw;

        try
        {
            raw = JsonSerializer.Deserialize<RawAnalysis>(
                rawJson,
                Options);
        }
        catch (JsonException ex)
        {
            throw new AiAnalysisParsingException(
                "The model response was not valid JSON.",
                ex);
        }

        if (raw is null)
        {
            throw new AiAnalysisParsingException(
                "The model response deserialized to null.");
        }

        if (string.IsNullOrWhiteSpace(raw.Summary))
        {
            throw new AiAnalysisParsingException(
                "The model response is missing 'summary'.");
        }

        if (!Enum.TryParse<IncidentSeverity>(
            raw.SeverityAssessment,
            ignoreCase: true,
            out var severity))
        {
            throw new AiAnalysisParsingException(
                $"Unknown severity value: '{raw.SeverityAssessment}'.");
        }

        var confidence = Math.Clamp(raw.Confidence, 0d, 1d);

        return new IncidentAiAnalysis(
            raw.Summary,
            severity,
            raw.Category ?? "Unclassified",
            raw.Indicators ?? [],
            raw.RecommendedActions ?? [],
            confidence,
            modelId,
            promptVersion);
    }

    private sealed class RawAnalysis
    {
        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("severityAssessment")]
        public string? SeverityAssessment { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("indicators")]
        public List<string>? Indicators { get; set; }

        [JsonPropertyName("recommendedActions")]
        public List<string>? RecommendedActions { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }
    }
}
