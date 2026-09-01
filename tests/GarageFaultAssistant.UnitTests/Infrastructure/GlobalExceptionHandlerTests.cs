using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using GarageFaultAssistant.Api.Application.AnalyseFault;
using GarageFaultAssistant.Api.Domain;
using GarageFaultAssistant.Api.Infrastructure.ExceptionHandling;
using Microsoft.AspNetCore.Http;

namespace GarageFaultAssistant.UnitTests.Infrastructure;

public class GlobalExceptionHandlerTests
{
    private readonly GlobalExceptionHandler _handler = new();

    [Fact]
    public async Task TryHandleAsync_ValidationException_returns_400_with_errors()
    {
        var exception = new ValidationException([
            new ValidationFailure("Description", "Description is required.")
        ]);

        var (status, body) = await HandleAsync(exception);

        Assert.Equal(StatusCodes.Status400BadRequest, status);
        Assert.Equal(GlobalExceptionHandler.ProblemTypeBase + "validation", body.GetProperty("type").GetString());
        Assert.True(body.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("Description", out _));
        Assert.True(body.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }

    [Fact]
    public async Task TryHandleAsync_FaultAnalysisRejectedException_returns_422()
    {
        var exception = new FaultAnalysisRejectedException(
            "The analysis result contained an unsupported vehicle system.");

        var (status, body) = await HandleAsync(exception);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, status);
        Assert.Equal(
            GlobalExceptionHandler.ProblemTypeBase + "fault-analysis-rejected",
            body.GetProperty("type").GetString());
        Assert.Equal(exception.Message, body.GetProperty("detail").GetString());
        Assert.DoesNotContain("at ", body.GetProperty("detail").GetString());
        Assert.True(body.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task TryHandleAsync_AnalysisUnavailableException_returns_503()
    {
        var exception = new AnalysisUnavailableException("Fault analysis is temporarily unavailable.");

        var (status, body) = await HandleAsync(exception);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status);
        Assert.Equal(
            GlobalExceptionHandler.ProblemTypeBase + "analysis-unavailable",
            body.GetProperty("type").GetString());
        Assert.Equal(exception.Message, body.GetProperty("detail").GetString());
        Assert.DoesNotContain("at ", body.GetProperty("detail").GetString());
        Assert.True(body.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task TryHandleAsync_AnalysisTimeoutException_returns_504()
    {
        var exception = new AnalysisTimeoutException(
            "The fault analysis timed out before a result was available.");

        var (status, body) = await HandleAsync(exception);

        Assert.Equal(StatusCodes.Status504GatewayTimeout, status);
        Assert.Equal(
            GlobalExceptionHandler.ProblemTypeBase + "analysis-timeout",
            body.GetProperty("type").GetString());
        Assert.Equal(exception.Message, body.GetProperty("detail").GetString());
        Assert.DoesNotContain("at ", body.GetProperty("detail").GetString());
        Assert.True(body.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task TryHandleAsync_OperationCanceledException_when_not_aborted_returns_504()
    {
        var (status, body) = await HandleAsync(new OperationCanceledException());

        Assert.Equal(StatusCodes.Status504GatewayTimeout, status);
        Assert.Equal(
            GlobalExceptionHandler.ProblemTypeBase + "analysis-timeout",
            body.GetProperty("type").GetString());
        Assert.True(body.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task TryHandleAsync_unhandled_exception_returns_500_without_exception_message()
    {
        const string secret = "SECRET_INTERNAL_STACK_MESSAGE_xyz";
        var exception = new InvalidOperationException(secret);

        var (status, body) = await HandleAsync(exception);
        var json = body.GetRawText();

        Assert.Equal(StatusCodes.Status500InternalServerError, status);
        Assert.Equal(GlobalExceptionHandler.ProblemTypeBase + "internal", body.GetProperty("type").GetString());
        Assert.DoesNotContain(secret, json);
        Assert.Equal(
            "An unexpected error occurred. Please try again later.",
            body.GetProperty("detail").GetString());
        Assert.True(body.TryGetProperty("traceId", out _));
    }

    private async Task<(int StatusCode, JsonElement Body)> HandleAsync(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.TraceIdentifier = "test-trace-id";

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);
        Assert.True(handled);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return (context.Response.StatusCode, document.RootElement.Clone());
    }
}
