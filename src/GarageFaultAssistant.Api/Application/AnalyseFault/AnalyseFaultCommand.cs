using GarageFaultAssistant.Api.Application.Common;
using MediatR;

namespace GarageFaultAssistant.Api.Application.AnalyseFault;

public record AnalyseFaultCommand(string Description)
    : IRequest<AnalyseFaultResult>, IHasDescription;
