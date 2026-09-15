namespace SentinelCase.AI;

public sealed class AiAnalysisParsingException : Exception
{
    public AiAnalysisParsingException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}
