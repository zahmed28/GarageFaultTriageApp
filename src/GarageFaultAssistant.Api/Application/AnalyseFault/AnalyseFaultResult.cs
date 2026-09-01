namespace GarageFaultAssistant.Api.Application.AnalyseFault;

public sealed class AnalyseFaultResult
{
    public required string CustomerConcern { get; init; }
    public required string VehicleSystem { get; init; }
    public required string Urgency { get; init; }
    public required IReadOnlyList<string> Symptoms { get; init; }
    public required IReadOnlyList<string> WorkshopChecks { get; init; }
    public required IReadOnlyList<string> ClarifyingQuestions { get; init; }
    public string? SafetyWarning { get; init; }
}
