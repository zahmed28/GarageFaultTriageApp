namespace GarageFaultAssistant.Api.Domain;

public sealed class FaultAnalysisRejectedException : Exception
{
    public FaultAnalysisRejectedException(string message)
        : base(message)
    {
    }
}
