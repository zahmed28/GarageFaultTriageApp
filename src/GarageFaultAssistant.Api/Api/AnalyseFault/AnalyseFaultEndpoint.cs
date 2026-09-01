using GarageFaultAssistant.Api.Application.AnalyseFault;
using MediatR;

namespace GarageFaultAssistant.Api.Api.AnalyseFault;

public static class AnalyseFaultEndpoint
{
    public static IEndpointRouteBuilder MapAnalyseFault(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/fault-assessments/analyse",
            async (AnalyseFaultRequest req, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new AnalyseFaultCommand(req.Description), ct);
                return Results.Ok(AnalyseFaultMapping.ToResponse(result));
            });
        return app;
    }
}
