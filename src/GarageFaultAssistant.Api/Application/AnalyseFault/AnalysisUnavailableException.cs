namespace GarageFaultAssistant.Api.Application.AnalyseFault;

public sealed class AnalysisUnavailableException : Exception
{
    public AnalysisUnavailableException(string message)
        : base(message)
    {
    }

    public AnalysisUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
