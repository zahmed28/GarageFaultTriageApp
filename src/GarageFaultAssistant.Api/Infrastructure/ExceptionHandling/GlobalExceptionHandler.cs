using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using GarageFaultAssistant.Api.Application.AnalyseFault;
using GarageFaultAssistant.Api.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GarageFaultAssistant.Api.Infrastructure.ExceptionHandling;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public const string ProblemTypeBase = "https://garagefault.app/problems/";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, problem) = MapException(httpContext, exception);

        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        // Serialize concrete type so ValidationProblemDetails.Errors is included.
        if (problem is ValidationProblemDetails validationProblem)
        {
            await httpContext.Response.WriteAsJsonAsync(
                validationProblem,
                SerializerOptions,
                cancellationToken);
        }
        else
        {
            await httpContext.Response.WriteAsJsonAsync(
                problem,
                SerializerOptions,
                cancellationToken);
        }

        return true;
    }

    private static (int StatusCode, ProblemDetails Problem) MapException(
        HttpContext httpContext,
        Exception exception)
    {
        switch (exception)
        {
            case ValidationException validationException:
            {
                var errors = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());

                var problem = new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation failed",
                    Detail = "One or more validation errors occurred.",
                    Type = ProblemTypeBase + "validation"
                };

                return (StatusCodes.Status400BadRequest, problem);
            }

            case FaultAnalysisRejectedException rejected:
                return (
                    StatusCodes.Status422UnprocessableEntity,
                    CreateProblem(
                        StatusCodes.Status422UnprocessableEntity,
                        "fault-analysis-rejected",
                        "Fault analysis rejected",
                        rejected.Message));

            case AnalysisUnavailableException unavailable:
                return (
                    StatusCodes.Status503ServiceUnavailable,
                    CreateProblem(
                        StatusCodes.Status503ServiceUnavailable,
                        "analysis-unavailable",
                        "Analysis unavailable",
                        unavailable.Message));

            case AnalysisTimeoutException timeout:
                return (
                    StatusCodes.Status504GatewayTimeout,
                    CreateProblem(
                        StatusCodes.Status504GatewayTimeout,
                        "analysis-timeout",
                        "Analysis timed out",
                        timeout.Message));

            case OperationCanceledException
                when !httpContext.RequestAborted.IsCancellationRequested:
                return (
                    StatusCodes.Status504GatewayTimeout,
                    CreateProblem(
                        StatusCodes.Status504GatewayTimeout,
                        "analysis-timeout",
                        "Analysis timed out",
                        "The fault analysis timed out before a result was available."));

            default:
                return (
                    StatusCodes.Status500InternalServerError,
                    CreateProblem(
                        StatusCodes.Status500InternalServerError,
                        "internal",
                        "An unexpected error occurred",
                        "An unexpected error occurred. Please try again later."));
        }
    }

    private static ProblemDetails CreateProblem(
        int status,
        string typeFragment,
        string title,
        string detail) =>
        new()
        {
            Status = status,
            Type = ProblemTypeBase + typeFragment,
            Title = title,
            Detail = detail
        };
}
