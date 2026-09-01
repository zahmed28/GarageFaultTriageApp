using System.Text.Json;
using System.Text.Json.Serialization;
using GarageFaultAssistant.Api.Application.AnalyseFault;
using GarageFaultAssistant.Api.Domain;

namespace GarageFaultAssistant.UnitTests.Application;

public class AnalyseFaultMappingTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public void ToResponse_maps_all_fields()
    {
        var result = new AnalyseFaultResult
        {
            CustomerConcern = "Engine hesitation under load",
            VehicleSystem = "Engine",
            Urgency = "High",
            Symptoms = ["Rough idle", "Hesitation"],
            WorkshopChecks = ["Check spark plugs", "Scan for codes"],
            ClarifyingQuestions = ["When does it occur?"],
            SafetyWarning = null
        };

        var response = AnalyseFaultMapping.ToResponse(result);

        Assert.Equal(result.CustomerConcern, response.CustomerConcern);
        Assert.Equal(result.VehicleSystem, response.VehicleSystem);
        Assert.Equal(result.Urgency, response.Urgency);
        Assert.Equal(result.Symptoms, response.Symptoms);
        Assert.Equal(result.WorkshopChecks, response.WorkshopChecks);
        Assert.Equal(result.ClarifyingQuestions, response.ClarifyingQuestions);
        Assert.Null(response.SafetyWarning);
    }

    [Fact]
    public void ToResponse_serialized_omits_null_safetyWarning()
    {
        var response = AnalyseFaultMapping.ToResponse(new AnalyseFaultResult
        {
            CustomerConcern = "General vehicle fault reported by customer.",
            VehicleSystem = "Body",
            Urgency = "Low",
            Symptoms = ["Customer reported fault"],
            WorkshopChecks = ["Visual inspection", "Road test if safe"],
            ClarifyingQuestions = ["When did the issue first occur?"],
            SafetyWarning = null
        });

        var json = JsonSerializer.Serialize(response, SerializerOptions);

        Assert.DoesNotContain("safetyWarning", json);
    }

    [Fact]
    public void ToResponse_serialized_includes_safetyWarning_when_set()
    {
        var warning = FaultAssessmentFactory.BrakesCriticalSafetyWarning;
        var response = AnalyseFaultMapping.ToResponse(new AnalyseFaultResult
        {
            CustomerConcern = "Brake pedal travels to the floor; vehicle does not stop.",
            VehicleSystem = "Brakes",
            Urgency = "Critical",
            Symptoms = ["Pedal to floor", "No braking"],
            WorkshopChecks = ["Inspect brake fluid level and leaks", "Check master cylinder"],
            ClarifyingQuestions = ["Did any warning light appear?"],
            SafetyWarning = warning
        });

        var json = JsonSerializer.Serialize(response, SerializerOptions);
        using var document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.TryGetProperty("safetyWarning", out var safetyWarning));
        Assert.Equal(warning, safetyWarning.GetString());
    }
}
