using GarageFaultAssistant.Api.Application.AnalyseFault;
using GarageFaultAssistant.Api.Domain;
using GarageFaultAssistant.Api.Infrastructure.Ai;
using GarageFaultAssistant.UnitTests.TestDoubles;
using Microsoft.Extensions.Options;

namespace GarageFaultAssistant.UnitTests.Application;

public class AnalyseFaultHandlerTests
{
    private const string ValidDescription =
        "Customer says the pedal goes to the floor and the car will not stop.";

    [Fact]
    public async Task Handle_happy_path_returns_mapped_result()
    {
        var candidate = ValidCandidate(
            vehicleSystem: "Engine",
            urgency: "High",
            customerConcern: "Engine hesitation under load");
        var handler = CreateHandler(new ConfigurableFaultAnalysisEngine(candidate));

        var result = await handler.Handle(
            new AnalyseFaultCommand(ValidDescription),
            CancellationToken.None);

        Assert.Equal("Engine hesitation under load", result.CustomerConcern);
        Assert.Equal("Engine", result.VehicleSystem);
        Assert.Equal("High", result.Urgency);
        Assert.Equal(["Rough idle", "Hesitation"], result.Symptoms);
        Assert.Equal(["Check spark plugs", "Scan for codes"], result.WorkshopChecks);
        Assert.Equal(["When does it occur?"], result.ClarifyingQuestions);
        Assert.Null(result.SafetyWarning);
    }

    [Fact]
    public async Task Handle_rejecting_engine_throws_FaultAnalysisRejectedException()
    {
        var handler = CreateHandler(new RejectingFaultAnalysisEngine());

        await Assert.ThrowsAsync<FaultAnalysisRejectedException>(() =>
            handler.Handle(new AnalyseFaultCommand(ValidDescription), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_unavailable_engine_throws_AnalysisUnavailableException()
    {
        var handler = CreateHandler(new UnavailableFaultAnalysisEngine());

        await Assert.ThrowsAsync<AnalysisUnavailableException>(() =>
            handler.Handle(new AnalyseFaultCommand(ValidDescription), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_timeout_throws_AnalysisTimeoutException()
    {
        var handler = CreateHandler(
            new TimeoutFaultAnalysisEngine(),
            timeoutSeconds: 1);

        await Assert.ThrowsAsync<AnalysisTimeoutException>(() =>
            handler.Handle(new AnalyseFaultCommand(ValidDescription), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_brakes_critical_includes_safety_warning()
    {
        var candidate = ValidCandidate(
            vehicleSystem: "Brakes",
            urgency: "Critical",
            customerConcern: "Brake pedal travels to the floor; vehicle does not stop.",
            symptoms: ["Pedal to floor", "No braking"],
            workshopChecks: ["Inspect brake fluid level and leaks", "Check master cylinder"],
            clarifyingQuestions: ["Did any warning light appear?"]);
        var handler = CreateHandler(new ConfigurableFaultAnalysisEngine(candidate));

        var result = await handler.Handle(
            new AnalyseFaultCommand(ValidDescription),
            CancellationToken.None);

        Assert.Equal(FaultAssessmentFactory.BrakesCriticalSafetyWarning, result.SafetyWarning);
    }

    private static AnalyseFaultHandler CreateHandler(
        IFaultAnalysisEngine engine,
        int timeoutSeconds = 30)
    {
        var options = Options.Create(new AiOptions
        {
            Provider = "Fake",
            TimeoutSeconds = timeoutSeconds
        });

        return new AnalyseFaultHandler(engine, options);
    }

    private static FaultAnalysisCandidate ValidCandidate(
        string vehicleSystem,
        string urgency,
        string customerConcern,
        IReadOnlyList<string>? symptoms = null,
        IReadOnlyList<string>? workshopChecks = null,
        IReadOnlyList<string>? clarifyingQuestions = null) =>
        new()
        {
            CustomerConcern = customerConcern,
            VehicleSystem = vehicleSystem,
            Urgency = urgency,
            Symptoms = symptoms ?? ["Rough idle", "Hesitation"],
            WorkshopChecks = workshopChecks ?? ["Check spark plugs", "Scan for codes"],
            ClarifyingQuestions = clarifyingQuestions ?? ["When does it occur?"]
        };
}
