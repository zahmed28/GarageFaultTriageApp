using GarageFaultAssistant.Api.Application.AnalyseFault;

namespace GarageFaultAssistant.Api.Tests.TestDoubles;

public sealed class TimeoutFaultAnalysisEngine : IFaultAnalysisEngine
{
    public async Task<FaultAnalysisCandidate> AnalyseAsync(
        string faultDescription,
        CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        throw new OperationCanceledException(cancellationToken);
    }
}
