using SentinelCase.Application.Common.Models;

namespace SentinelCase.Application.Common.Interfaces;

public interface IIncidentAiAnalysisService
{
    Task<IncidentAiAnalysis> AnalyzeAsync(
        IncidentAiAnalysisInput input,
        CancellationToken cancellationToken);
}
