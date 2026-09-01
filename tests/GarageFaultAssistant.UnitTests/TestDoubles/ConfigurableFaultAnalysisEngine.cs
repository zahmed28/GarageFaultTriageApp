using GarageFaultAssistant.Api.Application.AnalyseFault;

namespace GarageFaultAssistant.UnitTests.TestDoubles;

public sealed class ConfigurableFaultAnalysisEngine : IFaultAnalysisEngine
{
    private readonly FaultAnalysisCandidate _candidate;

    public ConfigurableFaultAnalysisEngine(FaultAnalysisCandidate candidate)
    {
        _candidate = candidate;
    }

    public Task<FaultAnalysisCandidate> AnalyseAsync(
        string faultDescription,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_candidate);
    }
}
