using MediatR;

using SentinelCase.Application.Common.Interfaces;
using SentinelCase.Application.Common.Models;

namespace SentinelCase.Application.Features.Incidents.Queries.AnalyzeIncidentWithAi;

public sealed class AnalyzeIncidentWithAiQueryHandler
    : IRequestHandler<AnalyzeIncidentWithAiQuery, AnalyzeIncidentWithAiResult?>
{
    private readonly ISecurityIncidentRepository _incidentRepository;
    private readonly IIncidentHistoryRepository _historyRepository;
    private readonly IIncidentNoteRepository _noteRepository;
    private readonly IIncidentAiAnalysisService _aiService;

    public AnalyzeIncidentWithAiQueryHandler(
        ISecurityIncidentRepository incidentRepository,
        IIncidentHistoryRepository historyRepository,
        IIncidentNoteRepository noteRepository,
        IIncidentAiAnalysisService aiService)
    {
        _incidentRepository = incidentRepository;
        _historyRepository = historyRepository;
        _noteRepository = noteRepository;
        _aiService = aiService;
    }

    public async Task<AnalyzeIncidentWithAiResult?> Handle(
        AnalyzeIncidentWithAiQuery request,
        CancellationToken cancellationToken)
    {
        var incident = await _incidentRepository.GetByIdAsync(
            request.IncidentId,
            cancellationToken);

        if (incident is null)
        {
            return null;
        }

        var notes = await _noteRepository.GetByIncidentIdAsync(
            request.IncidentId,
            cancellationToken);

        var history = await _historyRepository.GetByIncidentIdAsync(
            request.IncidentId,
            cancellationToken);

        var input = new IncidentAiAnalysisInput(
            incident.Id,
            incident.Title,
            incident.Description,
            incident.Severity,
            incident.Status,
            incident.DetectedAt,
            notes.Select(note => note.Content).ToArray(),
            history.Select(entry => entry.Description).ToArray());

        var analysis = await _aiService.AnalyzeAsync(
            input,
            cancellationToken);

        return new AnalyzeIncidentWithAiResult(
            incident.Id,
            analysis.Summary,
            analysis.SeverityAssessment,
            analysis.Category,
            analysis.Indicators,
            analysis.RecommendedActions,
            analysis.Confidence,
            analysis.ModelId,
            analysis.PromptVersion);
    }
}
