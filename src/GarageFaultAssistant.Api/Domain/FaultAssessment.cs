namespace GarageFaultAssistant.Api.Domain;

public sealed class FaultAssessment
{
    internal FaultAssessment(
        FaultDescription originalDescription,
        CustomerConcern customerConcern,
        VehicleSystem vehicleSystem,
        Urgency urgency,
        IReadOnlyList<Symptom> symptoms,
        IReadOnlyList<WorkshopCheck> workshopChecks,
        IReadOnlyList<ClarifyingQuestion> clarifyingQuestions,
        string? safetyWarning)
    {
        OriginalDescription = originalDescription;
        CustomerConcern = customerConcern;
        VehicleSystem = vehicleSystem;
        Urgency = urgency;
        Symptoms = symptoms;
        WorkshopChecks = workshopChecks;
        ClarifyingQuestions = clarifyingQuestions;
        SafetyWarning = safetyWarning;
    }

    public FaultDescription OriginalDescription { get; }
    public CustomerConcern CustomerConcern { get; }
    public VehicleSystem VehicleSystem { get; }
    public Urgency Urgency { get; }
    public IReadOnlyList<Symptom> Symptoms { get; }
    public IReadOnlyList<WorkshopCheck> WorkshopChecks { get; }
    public IReadOnlyList<ClarifyingQuestion> ClarifyingQuestions { get; }
    public string? SafetyWarning { get; }
}
