using GarageFaultAssistant.Api.Application.AnalyseFault;
using GarageFaultAssistant.Api.Infrastructure.Ai;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GarageFaultAssistant.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }

    public HttpClient CreateClientWithEngine(
        IFaultAnalysisEngine engine,
        int? timeoutSeconds = null)
    {
        return WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IFaultAnalysisEngine>();
                services.AddSingleton(engine);

                if (timeoutSeconds is int seconds)
                {
                    services.RemoveAll<IConfigureOptions<AiOptions>>();
                    services.RemoveAll<IOptions<AiOptions>>();
                    services.RemoveAll<IOptionsSnapshot<AiOptions>>();
                    services.RemoveAll<IOptionsMonitor<AiOptions>>();
                    services.Configure<AiOptions>(options =>
                    {
                        options.Provider = "Fake";
                        options.TimeoutSeconds = seconds;
                    });
                }
            });
        }).CreateClient();
    }
}
