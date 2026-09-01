using GarageFaultAssistant.Api.Application.AnalyseFault;

namespace GarageFaultAssistant.Api.Infrastructure.Ai;

public sealed class NotImplementedOpenAiFaultAnalysisEngine : IFaultAnalysisEngine
{
    public Task<FaultAnalysisCandidate> AnalyseAsync(
        string faultDescription,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException(
            "OpenAI fault analysis engine is not implemented yet. Set Ai:Provider to Fake, or complete T10.");
    }
}
