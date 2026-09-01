namespace GarageFaultAssistant.Api.Application.AnalyseFault;

public interface IFaultAnalysisEngine
{
    Task<FaultAnalysisCandidate> AnalyseAsync(
        string faultDescription,
        CancellationToken cancellationToken);
}
