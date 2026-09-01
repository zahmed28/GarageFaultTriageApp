using GarageFaultAssistant.Api.Domain;

namespace GarageFaultAssistant.UnitTests.Domain;

public class FaultAssessmentFactoryTests
{
    private static FaultDescription ValidDescription() =>
        FaultDescription.Create("Customer says the pedal goes to the floor.");

    private static FaultAssessment CreateValid(
        string vehicleSystem = "Engine",
        string urgency = "Medium",
        string customerConcern = "Engine hesitation under load",
        IReadOnlyList<string>? symptoms = null,
        IReadOnlyList<string>? workshopChecks = null,
        IReadOnlyList<string>? clarifyingQuestions = null)
    {
        return FaultAssessmentFactory.Create(
            ValidDescription(),
            customerConcern,
            vehicleSystem,
            urgency,
            symptoms ?? ["Rough idle", "Hesitation"],
            workshopChecks ?? ["Check spark plugs", "Scan for codes"],
            clarifyingQuestions ?? ["When does it occur?"]);
    }

    [Fact]
    public void Create_with_valid_fields_returns_assessment()
    {
        var assessment = CreateValid(
            vehicleSystem: "brakes",
            urgency: "HIGH",
            customerConcern: "Brake pedal feels soft",
            symptoms: ["Soft pedal"],
            workshopChecks: ["Check fluid level"],
            clarifyingQuestions: ["Any warning lights?"]);

        Assert.Equal("Brake pedal feels soft", assessment.CustomerConcern.Value);
        Assert.Equal(VehicleSystem.Brakes, assessment.VehicleSystem);
        Assert.Equal(Urgency.High, assessment.Urgency);
        Assert.Equal(["Soft pedal"], assessment.Symptoms.Select(s => s.Value));
        Assert.Equal(["Check fluid level"], assessment.WorkshopChecks.Select(c => c.Value));
        Assert.Equal(["Any warning lights?"], assessment.ClarifyingQuestions.Select(q => q.Value));
        Assert.Null(assessment.SafetyWarning);
        Assert.Equal(ValidDescription().Value, assessment.OriginalDescription.Value);
    }

    [Fact]
    public void Create_with_unknown_vehicle_system_throws()
    {
        Assert.Throws<FaultAnalysisRejectedException>(() =>
            CreateValid(vehicleSystem: "UnknownSystem"));
    }

    [Fact]
    public void Create_with_unknown_urgency_throws()
    {
        Assert.Throws<FaultAnalysisRejectedException>(() =>
            CreateValid(urgency: "Extreme"));
    }

    [Fact]
    public void Create_deduplicates_symptoms_ignore_case_keeping_first()
    {
        var assessment = CreateValid(
            symptoms: ["Pedal to floor", "PEDAL TO FLOOR", "No braking"]);

        Assert.Equal(["Pedal to floor", "No braking"], assessment.Symptoms.Select(s => s.Value));
    }

    [Fact]
    public void Create_with_empty_symptoms_after_dedupe_throws()
    {
        Assert.Throws<FaultAnalysisRejectedException>(() =>
            CreateValid(symptoms: ["  ", ""]));
    }

    [Fact]
    public void Create_with_empty_workshop_checks_after_dedupe_throws()
    {
        Assert.Throws<FaultAnalysisRejectedException>(() =>
            CreateValid(workshopChecks: []));
    }

    [Fact]
    public void Create_brakes_and_critical_sets_exact_safety_warning()
    {
        var assessment = CreateValid(vehicleSystem: "Brakes", urgency: "Critical");

        Assert.Equal(
            FaultAssessmentFactory.BrakesCriticalSafetyWarning,
            assessment.SafetyWarning);
        Assert.Equal(
            "Safety: treat as potential brake failure — do not drive; inspect before any other work.",
            assessment.SafetyWarning);
    }

    [Fact]
    public void Create_engine_and_critical_has_no_safety_warning()
    {
        var assessment = CreateValid(vehicleSystem: "Engine", urgency: "Critical");

        Assert.Null(assessment.SafetyWarning);
    }

    [Fact]
    public void Create_brakes_and_high_has_no_safety_warning()
    {
        var assessment = CreateValid(vehicleSystem: "Brakes", urgency: "High");

        Assert.Null(assessment.SafetyWarning);
    }

    [Fact]
    public void Create_with_customer_concern_over_500_chars_throws()
    {
        var concern = new string('x', 501);

        Assert.Throws<FaultAnalysisRejectedException>(() =>
            CreateValid(customerConcern: concern));
    }

    [Fact]
    public void Create_allows_empty_clarifying_questions()
    {
        var assessment = CreateValid(clarifyingQuestions: []);

        Assert.Empty(assessment.ClarifyingQuestions);
    }
}
