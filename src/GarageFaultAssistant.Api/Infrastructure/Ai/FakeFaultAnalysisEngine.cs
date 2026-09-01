using GarageFaultAssistant.Api.Application.AnalyseFault;

namespace GarageFaultAssistant.Api.Infrastructure.Ai;

public sealed class FakeFaultAnalysisEngine : IFaultAnalysisEngine
{
    private static readonly FixtureDefinition[] Fixtures =
    [
        new(
            "brakes-critical",
            ["brake", "pedal", "stop"],
            new FaultAnalysisCandidate
            {
                CustomerConcern = "Brake pedal travels to the floor; vehicle does not stop.",
                VehicleSystem = "Brakes",
                Urgency = "Critical",
                Symptoms = ["Pedal to floor", "No braking"],
                WorkshopChecks = ["Inspect brake fluid level and leaks", "Check master cylinder"],
                ClarifyingQuestions = ["Did any warning light appear?"]
            }),
        new(
            "engine-high",
            ["engine", "overheat", "smoke"],
            new FaultAnalysisCandidate
            {
                CustomerConcern = "Engine overheating with visible smoke.",
                VehicleSystem = "Engine",
                Urgency = "High",
                Symptoms = ["Temperature gauge high", "Smoke from bonnet"],
                WorkshopChecks = ["Check coolant level", "Inspect radiator and hoses"],
                ClarifyingQuestions = ["How long has the warning been on?"]
            }),
        new(
            "electrical-medium",
            ["battery", "electrical", "light"],
            new FaultAnalysisCandidate
            {
                CustomerConcern = "Electrical issue affecting starting or lights.",
                VehicleSystem = "Electrical",
                Urgency = "Medium",
                Symptoms = ["Dim headlights", "Slow crank"],
                WorkshopChecks = ["Test battery voltage", "Inspect alternator belt"],
                ClarifyingQuestions = ["Any recent battery replacement?"]
            }),
        new(
            "general-low",
            [],
            new FaultAnalysisCandidate
            {
                CustomerConcern = "General vehicle fault reported by customer.",
                VehicleSystem = "Body",
                Urgency = "Low",
                Symptoms = ["Customer reported fault"],
                WorkshopChecks = ["Visual inspection", "Road test if safe"],
                ClarifyingQuestions = ["When did the issue first occur?"]
            })
    ];

    public Task<FaultAnalysisCandidate> AnalyseAsync(
        string faultDescription,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var description = (faultDescription ?? string.Empty).Trim();

        foreach (var fixture in Fixtures)
        {
            if (fixture.Keywords.Length == 0)
            {
                continue;
            }

            if (fixture.Keywords.Any(keyword =>
                    description.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult(Clone(fixture.Candidate));
            }
        }

        var general = Fixtures[^1];
        return Task.FromResult(Clone(general.Candidate));
    }

    private static FaultAnalysisCandidate Clone(FaultAnalysisCandidate source) =>
        new()
        {
            CustomerConcern = source.CustomerConcern,
            VehicleSystem = source.VehicleSystem,
            Urgency = source.Urgency,
            Symptoms = source.Symptoms.ToList(),
            WorkshopChecks = source.WorkshopChecks.ToList(),
            ClarifyingQuestions = source.ClarifyingQuestions.ToList()
        };

    private sealed record FixtureDefinition(
        string Id,
        string[] Keywords,
        FaultAnalysisCandidate Candidate);
}
