using MediatR;

namespace SentinelCase.Application.Features.Incidents.Queries.AnalyzeIncidentWithAi;

public sealed record AnalyzeIncidentWithAiQuery(
    Guid IncidentId) : IRequest<AnalyzeIncidentWithAiResult?>;
