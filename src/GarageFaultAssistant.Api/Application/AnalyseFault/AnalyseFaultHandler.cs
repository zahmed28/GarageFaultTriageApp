using GarageFaultAssistant.Api.Domain;
using GarageFaultAssistant.Api.Infrastructure.Ai;
using MediatR;
using Microsoft.Extensions.Options;

namespace GarageFaultAssistant.Api.Application.AnalyseFault;

public sealed class AnalyseFaultHandler : IRequestHandler<AnalyseFaultCommand, AnalyseFaultResult>
{
    private readonly IFaultAnalysisEngine _engine;
    private readonly AiOptions _aiOptions;

    public AnalyseFaultHandler(
        IFaultAnalysisEngine engine,
        IOptions<AiOptions> aiOptions)
    {
        _engine = engine;
        _aiOptions = aiOptions.Value;
    }

    public async Task<AnalyseFaultResult> Handle(
        AnalyseFaultCommand request,
        CancellationToken cancellationToken)
    {
        FaultAnalysisCandidate candidate;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_aiOptions.TimeoutSeconds));

        try
        {
            candidate = await _engine.AnalyseAsync(request.Description, timeoutCts.Token);
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested
            && !cancellationToken.IsCancellationRequested)
        {
            throw new AnalysisTimeoutException(
                "The fault analysis timed out before a result was available.",
                ex);
        }
        catch (AnalysisUnavailableException)
        {
            throw;
        }
        catch (AnalysisTimeoutException)
        {
            throw;
        }
        catch (FaultAnalysisRejectedException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new AnalysisUnavailableException(
                "Fault analysis is temporarily unavailable.",
                ex);
        }

        var description = FaultDescription.Create(request.Description);
        var assessment = FaultAssessmentFactory.Create(
            description,
            candidate.CustomerConcern,
            candidate.VehicleSystem,
            candidate.Urgency,
            candidate.Symptoms,
            candidate.WorkshopChecks,
            candidate.ClarifyingQuestions);

        return AnalyseFaultMapping.ToResult(assessment);
    }
}
