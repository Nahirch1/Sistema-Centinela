using System.ClientModel;

using Microsoft.Extensions.Logging;

using OpenAI;
using OpenAI.Chat;

using SentinelCase.AI.Parsing;
using SentinelCase.AI.Prompts.IncidentAnalysis;
using SentinelCase.Application.Common.Interfaces;
using SentinelCase.Application.Common.Models;

namespace SentinelCase.AI.Services;

public sealed class GroqIncidentAiAnalysisService : IIncidentAiAnalysisService
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<GroqIncidentAiAnalysisService> _logger;
    private readonly string _modelId;

    public GroqIncidentAiAnalysisService(
        ChatClient chatClient,
        ILogger<GroqIncidentAiAnalysisService> logger,
        string modelId)
    {
        _chatClient = chatClient;
        _logger = logger;
        _modelId = modelId;
    }

    public async Task<IncidentAiAnalysis> AnalyzeAsync(
        IncidentAiAnalysisInput input,
        CancellationToken cancellationToken)
    {
        List<ChatMessage> messages =
        [
            new SystemChatMessage(IncidentAnalysisPromptV1.SystemPrompt),
            new UserChatMessage(BuildUserPrompt(input)),
        ];

        var options = new ChatCompletionOptions
        {
            MaxOutputTokenCount = 1024,
            Temperature = 0.2f,
        };

        var started = DateTimeOffset.UtcNow;

        ChatCompletion completion = await _chatClient.CompleteChatAsync(
            messages,
            options,
            cancellationToken);

        var latency = DateTimeOffset.UtcNow - started;

        var rawText = completion.Content.Count > 0
            ? completion.Content[0].Text
            : string.Empty;

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
