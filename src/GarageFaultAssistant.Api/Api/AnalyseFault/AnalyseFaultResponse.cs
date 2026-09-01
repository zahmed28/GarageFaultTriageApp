using System.Text.Json.Serialization;

namespace GarageFaultAssistant.Api.Api.AnalyseFault;

public sealed class AnalyseFaultResponse
{
    public required string CustomerConcern { get; init; }
    public required string VehicleSystem { get; init; }
    public required string Urgency { get; init; }
    public required IReadOnlyList<string> Symptoms { get; init; }
    public required IReadOnlyList<string> WorkshopChecks { get; init; }
    public required IReadOnlyList<string> ClarifyingQuestions { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SafetyWarning { get; init; }
}
