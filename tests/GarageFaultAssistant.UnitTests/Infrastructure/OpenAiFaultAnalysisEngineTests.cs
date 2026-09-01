using System.Net;
using System.Text;
using GarageFaultAssistant.Api.Application.AnalyseFault;
using GarageFaultAssistant.Api.Domain;
using GarageFaultAssistant.Api.Infrastructure.Ai;
using GarageFaultAssistant.Api.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GarageFaultAssistant.UnitTests.Infrastructure;

public class OpenAiFaultAnalysisEngineTests
{
    private const string Endpoint = "https://example.test/v1/chat/completions";
    private const string ValidCandidateJson =
        """
        {
          "customerConcern": "Engine hesitation under load",
          "vehicleSystem": "Engine",
          "urgency": "High",
          "symptoms": ["Rough idle", "Hesitation"],
          "workshopChecks": ["Check spark plugs", "Scan for codes"],
          "clarifyingQuestions": ["When does it occur?"]
        }
        """;

    [Fact]
    public async Task AnalyseAsync_valid_response_returns_candidate()
    {
        var handler = new StubHttpMessageHandler(_ =>
            SuccessChatResponse(ValidCandidateJson));
        var engine = CreateEngine(handler);

        var candidate = await engine.AnalyseAsync(
            "Customer reports engine hesitation under load.",
            CancellationToken.None);

        Assert.Equal("Engine hesitation under load", candidate.CustomerConcern);
        Assert.Equal("Engine", candidate.VehicleSystem);
        Assert.Equal("High", candidate.Urgency);
        Assert.Equal(["Rough idle", "Hesitation"], candidate.Symptoms);
        Assert.Equal(["Check spark plugs", "Scan for codes"], candidate.WorkshopChecks);
        Assert.Equal(["When does it occur?"], candidate.ClarifyingQuestions);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task AnalyseAsync_malformed_json_throws_FaultAnalysisRejectedException()
    {
        var handler = new StubHttpMessageHandler(_ =>
            SuccessChatResponse("{ not-valid-json"));
        var engine = CreateEngine(handler);

        await Assert.ThrowsAsync<FaultAnalysisRejectedException>(() =>
            engine.AnalyseAsync("Customer reports a fault description.", CancellationToken.None));
    }

    [Fact]
    public async Task AnalyseAsync_http_503_throws_AnalysisUnavailableException()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("unavailable")
            });
        var engine = CreateEngine(handler);

        await Assert.ThrowsAsync<AnalysisUnavailableException>(() =>
            engine.AnalyseAsync("Customer reports a fault description.", CancellationToken.None));
    }

    [Fact]
    public async Task AnalyseAsync_slow_response_throws_OperationCanceledException()
    {
        var handler = new StubHttpMessageHandler(async ct =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return SuccessChatResponse(ValidCandidateJson);
        });
        var engine = CreateEngine(handler, timeoutSeconds: 1);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            engine.AnalyseAsync("Customer reports a fault description.", CancellationToken.None));
    }

    [Fact]
    public void AddInfrastructure_with_OpenAI_registers_OpenAiFaultAnalysisEngine()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:Provider"] = "OpenAI",
                ["Ai:TimeoutSeconds"] = "30",
                ["Ai:OpenAI:Endpoint"] = Endpoint,
                ["Ai:OpenAI:ApiKey"] = "test-key",
                ["Ai:OpenAI:Model"] = "gpt-4o-mini"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<IFaultAnalysisEngine>();

        Assert.IsType<OpenAiFaultAnalysisEngine>(engine);
    }

    private static OpenAiFaultAnalysisEngine CreateEngine(
        StubHttpMessageHandler handler,
        int timeoutSeconds = 30)
    {
        var httpClient = new HttpClient(handler)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        var options = Options.Create(new AiOptions
        {
            Provider = "OpenAI",
            TimeoutSeconds = timeoutSeconds,
            OpenAI = new OpenAiOptions
            {
                Endpoint = Endpoint,
                ApiKey = "test-key",
                Model = "gpt-4o-mini"
            }
        });

        return new OpenAiFaultAnalysisEngine(
            httpClient,
            options,
            NullLogger<OpenAiFaultAnalysisEngine>.Instance);
    }

    private static HttpResponseMessage SuccessChatResponse(string contentJson)
    {
        var escaped = contentJson
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);

        var body =
            $$"""
            {
              "choices": [
                {
                  "message": {
                    "role": "assistant",
                    "content": "{{escaped}}"
                  }
                }
              ]
            }
            """;

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<CancellationToken, Task<HttpResponseMessage>> _responder;

        public StubHttpMessageHandler(Func<CancellationToken, HttpResponseMessage> responder)
            : this(ct => Task.FromResult(responder(ct)))
        {
        }

        public StubHttpMessageHandler(Func<CancellationToken, Task<HttpResponseMessage>> responder)
        {
            _responder = responder;
        }

        public int CallCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return await _responder(cancellationToken);
        }
    }
}
