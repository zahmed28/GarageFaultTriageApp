using GarageFaultAssistant.Api.Application.AnalyseFault;
using GarageFaultAssistant.Api.Infrastructure.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GarageFaultAssistant.Api.Infrastructure.DependencyInjection;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));

        var options = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>()
            ?? new AiOptions();

        ValidateOptions(options);

        var provider = options.Provider.Trim();
        if (string.Equals(provider, "Fake", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IFaultAnalysisEngine, FakeFaultAnalysisEngine>();
        }
        else if (string.Equals(provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IFaultAnalysisEngine, OpenAiFaultAnalysisEngine>(client =>
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
            });
        }
        else
        {
            throw new InvalidOperationException(
                $"Unsupported Ai:Provider '{options.Provider}'. Supported values: Fake, OpenAI.");
        }

        return services;
    }

    private static void ValidateOptions(AiOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            throw new InvalidOperationException("Ai:Provider is required and must be Fake or OpenAI.");
        }

        if (options.TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("Ai:TimeoutSeconds must be greater than 0.");
        }

        var provider = options.Provider.Trim();
        if (string.Equals(provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(options.OpenAI.Endpoint)
                || string.IsNullOrWhiteSpace(options.OpenAI.ApiKey)
                || string.IsNullOrWhiteSpace(options.OpenAI.Model))
            {
                throw new InvalidOperationException(
                    "When Ai:Provider is OpenAI, Ai:OpenAI:Endpoint, Ai:OpenAI:ApiKey, and Ai:OpenAI:Model are required.");
            }
        }
        else if (!string.Equals(provider, "Fake", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unsupported Ai:Provider '{options.Provider}'. Supported values: Fake, OpenAI.");
        }
    }
}
