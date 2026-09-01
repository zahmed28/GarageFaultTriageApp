namespace GarageFaultAssistant.Api.Infrastructure.Ai;

public sealed class OpenAiOptions
{
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string? Model { get; set; }
}
