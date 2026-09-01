using GarageFaultAssistant.Api.Application.AnalyseFault;
using GarageFaultAssistant.Api.Infrastructure.Ai;
using GarageFaultAssistant.Api.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GarageFaultAssistant.UnitTests.Infrastructure;

public class FakeFaultAnalysisEngineTests
{
    private readonly FakeFaultAnalysisEngine _engine = new();

    [Theory]
    [InlineData("Customer says the BRAKE is soft")]
    [InlineData("Pedal feels spongy")]
    [InlineData("Car will not STOP")]
    public async Task AnalyseAsync_brakes_keywords_return_brakes_critical_fixture(string description)
    {
        var candidate = await _engine.AnalyseAsync(description, CancellationToken.None);

        AssertCandidate(
            candidate,
            "Brake pedal travels to the floor; vehicle does not stop.",
            "Brakes",
            "Critical",
            ["Pedal to floor", "No braking"],
            ["Inspect brake fluid level and leaks", "Check master cylinder"],
            ["Did any warning light appear?"]);
    }

    [Theory]
    [InlineData("ENGINE noise when cold")]
    [InlineData("Car started to overheat")]
    [InlineData("Smoke from under the hood")]
    public async Task AnalyseAsync_engine_keywords_return_engine_high_fixture(string description)
    {
        var candidate = await _engine.AnalyseAsync(description, CancellationToken.None);

        AssertCandidate(
            candidate,
            "Engine overheating with visible smoke.",
            "Engine",
            "High",
            ["Temperature gauge high", "Smoke from bonnet"],
            ["Check coolant level", "Inspect radiator and hoses"],
            ["How long has the warning been on?"]);
    }

    [Theory]
    [InlineData("Battery is flat")]
    [InlineData("ELECTRICAL problem")]
    [InlineData("Headlight is dim")]
    public async Task AnalyseAsync_electrical_keywords_return_electrical_medium_fixture(string description)
    {
        var candidate = await _engine.AnalyseAsync(description, CancellationToken.None);

        AssertCandidate(
            candidate,
            "Electrical issue affecting starting or lights.",
            "Electrical",
            "Medium",
            ["Dim headlights", "Slow crank"],
            ["Test battery voltage", "Inspect alternator belt"],
            ["Any recent battery replacement?"]);
    }

    [Fact]
    public async Task AnalyseAsync_no_keyword_match_returns_general_low_fixture()
    {
        var candidate = await _engine.AnalyseAsync(
            "Something odd happened on the motorway yesterday.",
            CancellationToken.None);

        AssertCandidate(
            candidate,
            "General vehicle fault reported by customer.",
            "Body",
            "Low",
            ["Customer reported fault"],
            ["Visual inspection", "Road test if safe"],
            ["When did the issue first occur?"]);
    }

    [Fact]
    public async Task AnalyseAsync_first_fixture_wins_when_multiple_keywords_present()
    {
        // Contains "brake" (brakes) and "engine" — brakes is evaluated first.
        var candidate = await _engine.AnalyseAsync(
            "Brake issue after engine work",
            CancellationToken.None);

        Assert.Equal("Brakes", candidate.VehicleSystem);
        Assert.Equal("Critical", candidate.Urgency);
    }

    [Fact]
    public void AddInfrastructure_with_fake_provider_resolves_FakeFaultAnalysisEngine()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:Provider"] = "Fake",
                ["Ai:TimeoutSeconds"] = "30"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<IFaultAnalysisEngine>();

        Assert.IsType<FakeFaultAnalysisEngine>(engine);
    }

    private static void AssertCandidate(
        FaultAnalysisCandidate candidate,
        string customerConcern,
        string vehicleSystem,
        string urgency,
        string[] symptoms,
        string[] workshopChecks,
        string[] clarifyingQuestions)
    {
        Assert.Equal(customerConcern, candidate.CustomerConcern);
        Assert.Equal(vehicleSystem, candidate.VehicleSystem);
        Assert.Equal(urgency, candidate.Urgency);
        Assert.Equal(symptoms, candidate.Symptoms);
        Assert.Equal(workshopChecks, candidate.WorkshopChecks);
        Assert.Equal(clarifyingQuestions, candidate.ClarifyingQuestions);
    }
}
