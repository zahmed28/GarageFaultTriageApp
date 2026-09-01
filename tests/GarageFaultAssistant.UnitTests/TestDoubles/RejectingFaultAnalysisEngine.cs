using GarageFaultAssistant.Api.Application.AnalyseFault;

namespace GarageFaultAssistant.UnitTests.TestDoubles;

public sealed class RejectingFaultAnalysisEngine : IFaultAnalysisEngine
{
    public Task<FaultAnalysisCandidate> AnalyseAsync(
        string faultDescription,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new FaultAnalysisCandidate
        {
            CustomerConcern = "Invalid candidate",
            VehicleSystem = "UnknownSystem",
            Urgency = "Critical",
            Symptoms = ["Symptom"],
            WorkshopChecks = ["Check"],
            ClarifyingQuestions = []
        });
    }
}
