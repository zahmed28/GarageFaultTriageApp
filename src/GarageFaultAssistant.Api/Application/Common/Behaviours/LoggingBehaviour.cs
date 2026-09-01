using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GarageFaultAssistant.Api.Application.Common.Behaviours;

public sealed class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

    public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next(cancellationToken);
            stopwatch.Stop();

            if (request is IHasDescription hasDescription)
            {
                _logger.LogInformation(
                    "Handled {RequestName} successfully in {ElapsedMs} ms. DescriptionLength={DescriptionLength}",
                    requestName,
                    stopwatch.ElapsedMilliseconds,
                    hasDescription.Description.Length);
            }
            else
            {
                _logger.LogInformation(
                    "Handled {RequestName} successfully in {ElapsedMs} ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception)
        {
            stopwatch.Stop();

            if (request is IHasDescription hasDescription)
            {
                _logger.LogError(
                    "Failed {RequestName} after {ElapsedMs} ms. DescriptionLength={DescriptionLength}",
                    requestName,
                    stopwatch.ElapsedMilliseconds,
                    hasDescription.Description.Length);
            }
            else
            {
                _logger.LogError(
                    "Failed {RequestName} after {ElapsedMs} ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);
            }

            throw;
        }
    }
}
