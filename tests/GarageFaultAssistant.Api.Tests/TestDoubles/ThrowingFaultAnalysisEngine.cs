using GarageFaultAssistant.Api.Application.AnalyseFault;

namespace GarageFaultAssistant.Api.Tests.TestDoubles;

public sealed class ThrowingFaultAnalysisEngine : IFaultAnalysisEngine
{
    public const string SecretMessage = "SECRET_INTERNAL_STACK_MESSAGE_xyz";

    public Task<FaultAnalysisCandidate> AnalyseAsync(
        string faultDescription,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(SecretMessage);
    }
}
