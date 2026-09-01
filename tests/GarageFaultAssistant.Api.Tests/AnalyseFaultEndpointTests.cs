using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GarageFaultAssistant.Api.Domain;
using GarageFaultAssistant.Api.Infrastructure.ExceptionHandling;
using GarageFaultAssistant.Api.Tests.TestDoubles;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace GarageFaultAssistant.Api.Tests;

public class AnalyseFaultEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string AnalysePath = "/api/fault-assessments/analyse";

    private readonly CustomWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AnalyseFaultEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Analyse_with_9_char_description_returns_400_validation()
    {
        var client = _factory.CreateClient();

        using var response = await PostAnalyseAsync(client, "123456789");
        using var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            GlobalExceptionHandler.ProblemTypeBase + "validation",
            body.RootElement.GetProperty("type").GetString());
        Assert.True(body.RootElement.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("Description", out _));
        AssertTraceId(body.RootElement);
    }

    [Fact]
    public async Task Analyse_brakes_keyword_returns_fixture_with_safetyWarning()
    {
        var client = _factory.CreateClient();

        using var response = await PostAnalyseAsync(
            client,
            "Customer says the brake pedal goes to the floor.");
        using var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertFixture(
            body.RootElement,
            customerConcern: "Brake pedal travels to the floor; vehicle does not stop.",
            vehicleSystem: "Brakes",
            urgency: "Critical",
            symptoms: ["Pedal to floor", "No braking"],
            workshopChecks: ["Inspect brake fluid level and leaks", "Check master cylinder"],
            clarifyingQuestions: ["Did any warning light appear?"],
            safetyWarning: FaultAssessmentFactory.BrakesCriticalSafetyWarning);
    }

    [Fact]
    public async Task Analyse_engine_keyword_returns_engine_high_without_safetyWarning()
    {
        var client = _factory.CreateClient();

        using var response = await PostAnalyseAsync(
            client,
            "The engine is overheating with smoke.");
        using var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertFixture(
            body.RootElement,
            customerConcern: "Engine overheating with visible smoke.",
            vehicleSystem: "Engine",
            urgency: "High",
            symptoms: ["Temperature gauge high", "Smoke from bonnet"],
            workshopChecks: ["Check coolant level", "Inspect radiator and hoses"],
            clarifyingQuestions: ["How long has the warning been on?"],
            safetyWarning: null);
    }

    [Fact]
    public async Task Analyse_electrical_keyword_returns_electrical_medium_fixture()
    {
        var client = _factory.CreateClient();

        using var response = await PostAnalyseAsync(
            client,
            "Battery is flat and electrical issues.");
        using var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertFixture(
            body.RootElement,
            customerConcern: "Electrical issue affecting starting or lights.",
            vehicleSystem: "Electrical",
            urgency: "Medium",
            symptoms: ["Dim headlights", "Slow crank"],
            workshopChecks: ["Test battery voltage", "Inspect alternator belt"],
            clarifyingQuestions: ["Any recent battery replacement?"],
            safetyWarning: null);
    }

    [Fact]
    public async Task Analyse_generic_description_returns_general_low_fixture()
    {
        var client = _factory.CreateClient();

        using var response = await PostAnalyseAsync(
            client,
            "Something odd happened on the motorway yesterday.");
        using var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertFixture(
            body.RootElement,
            customerConcern: "General vehicle fault reported by customer.",
            vehicleSystem: "Body",
            urgency: "Low",
            symptoms: ["Customer reported fault"],
            workshopChecks: ["Visual inspection", "Road test if safe"],
            clarifyingQuestions: ["When did the issue first occur?"],
            safetyWarning: null);
    }

    [Fact]
    public async Task Analyse_with_rejecting_engine_returns_422()
    {
        var client = _factory.CreateClientWithEngine(new RejectingFaultAnalysisEngine());

        using var response = await PostAnalyseAsync(
            client,
            "Customer reports a fault that should be rejected.");
        using var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(
            GlobalExceptionHandler.ProblemTypeBase + "fault-analysis-rejected",
            body.RootElement.GetProperty("type").GetString());
        AssertTraceId(body.RootElement);
    }

    [Fact]
    public async Task Analyse_with_unavailable_engine_returns_503()
    {
        var client = _factory.CreateClientWithEngine(new UnavailableFaultAnalysisEngine());

        using var response = await PostAnalyseAsync(
            client,
            "Customer reports a fault while engine is down.");
        using var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(
            GlobalExceptionHandler.ProblemTypeBase + "analysis-unavailable",
            body.RootElement.GetProperty("type").GetString());
        AssertTraceId(body.RootElement);
    }

    [Fact]
    public async Task Analyse_with_timeout_engine_returns_504()
    {
        var client = _factory.CreateClientWithEngine(
            new TimeoutFaultAnalysisEngine(),
            timeoutSeconds: 1);

        using var response = await PostAnalyseAsync(
            client,
            "Customer reports a fault that will time out.");
        using var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
        Assert.Equal(
            GlobalExceptionHandler.ProblemTypeBase + "analysis-timeout",
            body.RootElement.GetProperty("type").GetString());
        AssertTraceId(body.RootElement);
    }

    [Fact]
    public async Task Unhandled_exception_returns_500_without_secret_message()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IStartupFilter, UnhandledThrowStartupFilter>();
            });
        }).CreateClient();

        using var response = await client.GetAsync("/__test/unhandled");
        var json = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(json);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(
            GlobalExceptionHandler.ProblemTypeBase + "internal",
            body.RootElement.GetProperty("type").GetString());
        Assert.DoesNotContain(ThrowingFaultAnalysisEngine.SecretMessage, json);
        AssertTraceId(body.RootElement);
    }

    private static async Task<HttpResponseMessage> PostAnalyseAsync(HttpClient client, string description)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(new { description }),
            Encoding.UTF8,
            "application/json");
        return await client.PostAsync(AnalysePath, content);
    }

    private async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    private static void AssertTraceId(JsonElement body)
    {
        Assert.True(body.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }

    private static void AssertFixture(
        JsonElement body,
        string customerConcern,
        string vehicleSystem,
        string urgency,
        string[] symptoms,
        string[] workshopChecks,
        string[] clarifyingQuestions,
        string? safetyWarning)
    {
        Assert.Equal(customerConcern, body.GetProperty("customerConcern").GetString());
        Assert.Equal(vehicleSystem, body.GetProperty("vehicleSystem").GetString());
        Assert.Equal(urgency, body.GetProperty("urgency").GetString());
        Assert.Equal(symptoms, body.GetProperty("symptoms").EnumerateArray().Select(e => e.GetString()).ToArray());
        Assert.Equal(workshopChecks, body.GetProperty("workshopChecks").EnumerateArray().Select(e => e.GetString()).ToArray());
        Assert.Equal(clarifyingQuestions, body.GetProperty("clarifyingQuestions").EnumerateArray().Select(e => e.GetString()).ToArray());

        if (safetyWarning is null)
        {
            Assert.False(body.TryGetProperty("safetyWarning", out _));
        }
        else
        {
            Assert.Equal(safetyWarning, body.GetProperty("safetyWarning").GetString());
        }
    }

    private sealed class UnhandledThrowStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                // Register after the app pipeline so UseExceptionHandler remains outer.
                next(app);
                app.Map("/__test/unhandled", branch =>
                {
                    branch.Run(_ =>
                        throw new InvalidOperationException(ThrowingFaultAnalysisEngine.SecretMessage));
                });
            };
        }
    }
}
