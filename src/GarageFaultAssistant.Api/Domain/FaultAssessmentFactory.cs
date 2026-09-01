namespace GarageFaultAssistant.Api.Domain;

public static class FaultAssessmentFactory
{
    public const string BrakesCriticalSafetyWarning =
        "Safety: treat as potential brake failure — do not drive; inspect before any other work.";

    public static FaultAssessment Create(
        FaultDescription originalDescription,
        string customerConcern,
        string vehicleSystem,
        string urgency,
        IReadOnlyList<string> symptoms,
        IReadOnlyList<string> workshopChecks,
        IReadOnlyList<string> clarifyingQuestions)
    {
        ArgumentNullException.ThrowIfNull(originalDescription);
        ArgumentNullException.ThrowIfNull(symptoms);
        ArgumentNullException.ThrowIfNull(workshopChecks);
        ArgumentNullException.ThrowIfNull(clarifyingQuestions);

        var parsedVehicleSystem = ParseVehicleSystem(vehicleSystem);
        var parsedUrgency = ParseUrgency(urgency);

        var distinctSymptoms = Deduplicate(symptoms);
        var distinctWorkshopChecks = Deduplicate(workshopChecks);
        var distinctClarifyingQuestions = Deduplicate(clarifyingQuestions);

        if (distinctSymptoms.Count == 0)
        {
            throw new FaultAnalysisRejectedException(
                "At least one symptom is required.");
        }

        if (distinctWorkshopChecks.Count == 0)
        {
            throw new FaultAnalysisRejectedException(
                "At least one workshop check is required.");
        }

        var concern = CustomerConcern.Create(customerConcern);
        var symptomVos = distinctSymptoms.Select(Symptom.Create).ToList();
        var checkVos = distinctWorkshopChecks.Select(WorkshopCheck.Create).ToList();
        var questionVos = distinctClarifyingQuestions.Select(ClarifyingQuestion.Create).ToList();

        string? safetyWarning = null;
        if (parsedVehicleSystem == VehicleSystem.Brakes && parsedUrgency == Urgency.Critical)
        {
            safetyWarning = BrakesCriticalSafetyWarning;
        }

        return new FaultAssessment(
            originalDescription,
            concern,
            parsedVehicleSystem,
            parsedUrgency,
            symptomVos,
            checkVos,
            questionVos,
            safetyWarning);
    }

    private static VehicleSystem ParseVehicleSystem(string? value)
    {
        if (Enum.TryParse<VehicleSystem>(value, ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new FaultAnalysisRejectedException(
            "The analysis result contained an unsupported vehicle system.");
    }

    private static Urgency ParseUrgency(string? value)
    {
        if (Enum.TryParse<Urgency>(value, ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new FaultAnalysisRejectedException(
            "The analysis result contained an unsupported urgency level.");
    }

    private static List<string> Deduplicate(IReadOnlyList<string> values)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values)
        {
            var trimmed = (value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            if (seen.Add(trimmed))
            {
                result.Add(trimmed);
            }
        }

        return result;
    }
}
