namespace GarageFaultAssistant.Api.Application.AnalyseFault;

public sealed class AnalysisTimeoutException : Exception
{
    public AnalysisTimeoutException(string message)
        : base(message)
    {
    }

    public AnalysisTimeoutException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
