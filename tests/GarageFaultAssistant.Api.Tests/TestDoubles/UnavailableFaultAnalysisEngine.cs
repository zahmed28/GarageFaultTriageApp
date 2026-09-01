using GarageFaultAssistant.Api.Application.AnalyseFault;

namespace GarageFaultAssistant.Api.Tests.TestDoubles;

public sealed class UnavailableFaultAnalysisEngine : IFaultAnalysisEngine
{
    public Task<FaultAnalysisCandidate> AnalyseAsync(
        string faultDescription,
        CancellationToken cancellationToken)
    {
        throw new AnalysisUnavailableException("Fault analysis is temporarily unavailable.");
    }
}
