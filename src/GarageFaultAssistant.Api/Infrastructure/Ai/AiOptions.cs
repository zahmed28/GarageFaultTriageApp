namespace GarageFaultAssistant.Api.Infrastructure.Ai;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string Provider { get; set; } = "Fake";
    public int TimeoutSeconds { get; set; } = 30;
    public OpenAiOptions OpenAI { get; set; } = new();
}
