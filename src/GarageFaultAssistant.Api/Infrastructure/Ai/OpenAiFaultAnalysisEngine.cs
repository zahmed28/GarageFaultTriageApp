using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using GarageFaultAssistant.Api.Application.AnalyseFault;
using GarageFaultAssistant.Api.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GarageFaultAssistant.Api.Infrastructure.Ai;

public sealed class OpenAiFaultAnalysisEngine : IFaultAnalysisEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly Regex JsonBlockRegex = new(
        @"\{[\s\S]*\}",
        RegexOptions.Compiled);

    private readonly HttpClient _httpClient;
    private readonly AiOptions _options;
    private readonly ILogger<OpenAiFaultAnalysisEngine> _logger;

    public OpenAiFaultAnalysisEngine(
        HttpClient httpClient,
        IOptions<AiOptions> options,
        ILogger<OpenAiFaultAnalysisEngine> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<FaultAnalysisCandidate> AnalyseAsync(
        string faultDescription,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        var endpoint = _options.OpenAI.Endpoint
            ?? throw new AnalysisUnavailableException("OpenAI endpoint is not configured.");
        var apiKey = _options.OpenAI.ApiKey
            ?? throw new AnalysisUnavailableException("OpenAI API key is not configured.");
        var model = _options.OpenAI.Model
            ?? throw new AnalysisUnavailableException("OpenAI model is not configured.");

        var requestBody = new ChatCompletionRequest
        {
            Model = model,
            Temperature = 0,
            ResponseFormat = new ResponseFormat { Type = "json_object" },
            Messages =
            [
                new ChatMessage
                {
                    Role = "system",
                    Content =
                        """
                        You are a vehicle fault triage assistant. Return ONLY a JSON object with these fields:
                        customerConcern (string), vehicleSystem (one of: Engine, Electrical, Transmission, Suspension, Brakes, Cooling, Steering, Body),
                        urgency (one of: Low, Medium, High, Critical), symptoms (string array), workshopChecks (string array),
                        clarifyingQuestions (string array). Do not include safetyWarning. Do not wrap in markdown.
                        """
                },
                new ChatMessage
                {
                    Role = "user",
                    Content = faultDescription
                }
            ]
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(requestBody, options: JsonOptions);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested
            && !cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI fault analysis HTTP call failed.");
            throw new AnalysisUnavailableException(
                "Fault analysis is temporarily unavailable.",
                ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "OpenAI fault analysis returned HTTP {StatusCode}.",
                    (int)response.StatusCode);
                throw new AnalysisUnavailableException(
                    "Fault analysis is temporarily unavailable.");
            }

            string responseText;
            try
            {
                responseText = await response.Content.ReadAsStringAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read OpenAI response body.");
                throw new AnalysisUnavailableException(
                    "Fault analysis is temporarily unavailable.",
                    ex);
            }

            return ParseCandidate(responseText);
        }
    }

    private FaultAnalysisCandidate ParseCandidate(string responseText)
    {
        string? content;
        try
        {
            var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(responseText, JsonOptions);
            content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "OpenAI response was not valid chat-completion JSON.");
            throw new FaultAnalysisRejectedException(
                "The analysis result could not be interpreted.");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new FaultAnalysisRejectedException(
                "The analysis result could not be interpreted.");
        }

        var jsonPayload = ExtractJsonObject(content);
        try
        {
            var candidate = JsonSerializer.Deserialize<FaultAnalysisCandidate>(jsonPayload, JsonOptions);
            if (candidate is null
                || string.IsNullOrWhiteSpace(candidate.CustomerConcern)
                || string.IsNullOrWhiteSpace(candidate.VehicleSystem)
                || string.IsNullOrWhiteSpace(candidate.Urgency)
                || candidate.Symptoms is null
                || candidate.WorkshopChecks is null
                || candidate.ClarifyingQuestions is null)
            {
                throw new FaultAnalysisRejectedException(
                    "The analysis result could not be interpreted.");
            }

            return candidate;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "OpenAI message content was not a valid fault analysis candidate.");
            throw new FaultAnalysisRejectedException(
                "The analysis result could not be interpreted.");
        }
    }

    private static string ExtractJsonObject(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith('{'))
        {
            return trimmed;
        }

        var match = JsonBlockRegex.Match(trimmed);
        if (!match.Success)
        {
            throw new FaultAnalysisRejectedException(
                "The analysis result could not be interpreted.");
        }

        return match.Value;
    }

    private sealed class ChatCompletionRequest
    {
        public required string Model { get; init; }
        public required IReadOnlyList<ChatMessage> Messages { get; init; }
        public double Temperature { get; init; }

        [JsonPropertyName("response_format")]
        public ResponseFormat? ResponseFormat { get; init; }
    }

    private sealed class ResponseFormat
    {
        public required string Type { get; init; }
    }

    private sealed class ChatMessage
    {
        public required string Role { get; init; }
        public required string Content { get; init; }
    }

    private sealed class ChatCompletionResponse
    {
        public List<ChatChoice>? Choices { get; init; }
    }

    private sealed class ChatChoice
    {
        public ChatMessageResponse? Message { get; init; }
    }

    private sealed class ChatMessageResponse
    {
        public string? Content { get; init; }
    }
}
