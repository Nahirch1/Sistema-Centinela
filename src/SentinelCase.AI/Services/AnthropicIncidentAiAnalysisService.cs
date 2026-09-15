using System.Text;

using Anthropic;
using Anthropic.Models.Messages;

using Microsoft.Extensions.Logging;

using SentinelCase.AI.Parsing;
using SentinelCase.AI.Prompts.IncidentAnalysis;
using SentinelCase.Application.Common.Interfaces;
using SentinelCase.Application.Common.Models;

namespace SentinelCase.AI.Services;

public sealed class AnthropicIncidentAiAnalysisService : IIncidentAiAnalysisService
{
    private readonly AnthropicClient _client;
    private readonly ILogger<AnthropicIncidentAiAnalysisService> _logger;
    private readonly string _modelId;

    public AnthropicIncidentAiAnalysisService(
        AnthropicClient client,
        ILogger<AnthropicIncidentAiAnalysisService> logger,
        string modelId)
    {
        _client = client;
        _logger = logger;
        _modelId = modelId;
    }

    public async Task<IncidentAiAnalysis> AnalyzeAsync(
        IncidentAiAnalysisInput input,
        CancellationToken cancellationToken)
    {
        var userPrompt = BuildUserPrompt(input);

        var parameters = new MessageCreateParams
        {
            MaxTokens = 1024,
            Model = new Model(_modelId), // RIESGO: constructor de Model
            System = IncidentAnalysisPromptV1.SystemPrompt,
            Messages =
            [
                new()
                {
                    Role = Role.User,
                    Content = userPrompt,
                },
            ],
        };

        var started = DateTimeOffset.UtcNow;

        var message = await _client.Messages.Create(
            parameters,
            cancellationToken); // RIESGO: firma exacta de Create

        var latency = DateTimeOffset.UtcNow - started;

        var rawText = ExtractText(message);

        _logger.LogInformation(
            "AI incident analysis completed for {IncidentId} in {LatencyMs}ms using {Model}",
            input.IncidentId,
            latency.TotalMilliseconds,
            _modelId);

        return IncidentAiAnalysisJsonParser.Parse(
            rawText,
            _modelId,
            IncidentAnalysisPromptV1.Version);
    }

    private static string ExtractText(Message message)
    {
        var builder = new StringBuilder();

        foreach (var block in message.Content)
        {
            if (block.TryPickText(out var textBlock))
            {
                builder.Append(textBlock.Text);
            }
        }

        return builder.ToString();
    }

    private static string BuildUserPrompt(IncidentAiAnalysisInput input)
    {
        var notes = input.NoteContents.Count == 0
            ? "(sin notas)"
            : string.Join(
                Environment.NewLine,
                input.NoteContents.Select(note => $"- {note}"));

        var history = input.HistorySummaries.Count == 0
            ? "(sin historial)"
            : string.Join(
                Environment.NewLine,
                input.HistorySummaries.Select(entry => $"- {entry}"));

        return $"""
            Incidente: {input.IncidentId}
            Título: {input.Title}
            Descripción: {input.Description}
            Severidad actual: {input.Severity}
            Estado actual: {input.Status}
            Detectado: {input.DetectedAt:O}

            Notas de investigación:
            {notes}

            Historial:
            {history}
            """;
    }
}
