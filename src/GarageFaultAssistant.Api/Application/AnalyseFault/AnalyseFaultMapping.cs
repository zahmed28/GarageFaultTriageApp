using GarageFaultAssistant.Api.Api.AnalyseFault;
using GarageFaultAssistant.Api.Domain;

namespace GarageFaultAssistant.Api.Application.AnalyseFault;

public static class AnalyseFaultMapping
{
    public static AnalyseFaultResult ToResult(FaultAssessment assessment)
    {
        ArgumentNullException.ThrowIfNull(assessment);

        return new AnalyseFaultResult
        {
            CustomerConcern = assessment.CustomerConcern.Value,
            VehicleSystem = assessment.VehicleSystem.ToString(),
            Urgency = assessment.Urgency.ToString(),
            Symptoms = assessment.Symptoms.Select(s => s.Value).ToList(),
            WorkshopChecks = assessment.WorkshopChecks.Select(c => c.Value).ToList(),
            ClarifyingQuestions = assessment.ClarifyingQuestions.Select(q => q.Value).ToList(),
            SafetyWarning = assessment.SafetyWarning
        };
    }

    public static AnalyseFaultResponse ToResponse(AnalyseFaultResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new AnalyseFaultResponse
        {
            CustomerConcern = result.CustomerConcern,
            VehicleSystem = result.VehicleSystem,
            Urgency = result.Urgency,
            Symptoms = result.Symptoms,
            WorkshopChecks = result.WorkshopChecks,
            ClarifyingQuestions = result.ClarifyingQuestions,
            SafetyWarning = result.SafetyWarning
        };
    }
}
