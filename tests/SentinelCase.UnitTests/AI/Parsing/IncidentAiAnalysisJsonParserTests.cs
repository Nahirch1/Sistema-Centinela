using SentinelCase.AI;
using SentinelCase.AI.Parsing;
using SentinelCase.Domain.Enums;

namespace SentinelCase.UnitTests.AI.Parsing;

public sealed class IncidentAiAnalysisJsonParserTests
{
    private const string ModelId = "claude-test-model";
    private const string PromptVersion = "incident-analysis-v1";

    [Fact]
    public void Parse_WithValidJson_ShouldReturnAnalysis()
    {
        const string json = """
            {
              "summary": "Privileged login from an unrecognized address.",
              "severityAssessment": "High",
              "category": "Credential Access",
              "indicators": ["198.51.100.23"],
              "recommendedActions": ["Force password reset"],
              "confidence": 0.87
            }
            """;

        var result = IncidentAiAnalysisJsonParser.Parse(
            json,
            ModelId,
            PromptVersion);

        Assert.Equal(
            "Privileged login from an unrecognized address.",
            result.Summary);
        Assert.Equal(IncidentSeverity.High, result.SeverityAssessment);
        Assert.Equal("Credential Access", result.Category);
        Assert.Single(result.Indicators);
        Assert.Single(result.RecommendedActions);
        Assert.Equal(0.87, result.Confidence);
        Assert.Equal(ModelId, result.ModelId);
        Assert.Equal(PromptVersion, result.PromptVersion);
    }

    [Fact]
    public void Parse_WithConfidenceAboveOne_ShouldClampToOne()
    {
        const string json = """
            {
              "summary": "Test summary.",
              "severityAssessment": "Low",
              "category": "Other",
              "indicators": [],
              "recommendedActions": [],
              "confidence": 1.5
            }
            """;

        var result = IncidentAiAnalysisJsonParser.Parse(
            json,
            ModelId,
            PromptVersion);

        Assert.Equal(1.0, result.Confidence);
    }

    [Fact]
    public void Parse_WithEmptyResponse_ShouldThrowAiAnalysisParsingException()
    {
        Assert.Throws<AiAnalysisParsingException>(() =>
            IncidentAiAnalysisJsonParser.Parse(
                string.Empty,
                ModelId,
                PromptVersion));
    }

    [Fact]
    public void Parse_WithMalformedJson_ShouldThrowAiAnalysisParsingException()
    {
        const string json = "{ not valid json ";

        Assert.Throws<AiAnalysisParsingException>(() =>
            IncidentAiAnalysisJsonParser.Parse(
                json,
                ModelId,
                PromptVersion));
    }

    [Fact]
    public void Parse_WithMissingSummary_ShouldThrowAiAnalysisParsingException()
    {
        const string json = """
            {
              "severityAssessment": "Low",
              "category": "Other",
              "indicators": [],
              "recommendedActions": [],
              "confidence": 0.5
            }
            """;

        Assert.Throws<AiAnalysisParsingException>(() =>
            IncidentAiAnalysisJsonParser.Parse(
                json,
                ModelId,
                PromptVersion));
    }

    [Fact]
    public void Parse_WithUnknownSeverity_ShouldThrowAiAnalysisParsingException()
    {
        const string json = """
            {
              "summary": "Test summary.",
              "severityAssessment": "Catastrophic",
              "category": "Other",
              "indicators": [],
              "recommendedActions": [],
              "confidence": 0.5
            }
            """;

        Assert.Throws<AiAnalysisParsingException>(() =>
            IncidentAiAnalysisJsonParser.Parse(
                json,
                ModelId,
                PromptVersion));
    }
}
