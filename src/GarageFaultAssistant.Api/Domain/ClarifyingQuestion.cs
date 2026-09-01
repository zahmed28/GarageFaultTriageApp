namespace GarageFaultAssistant.Api.Domain;

public sealed class ClarifyingQuestion
{
    public const int MaxLength = 300;

    private ClarifyingQuestion(string value) => Value = value;

    public string Value { get; }

    public static ClarifyingQuestion Create(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxLength)
        {
            throw new FaultAnalysisRejectedException(
                "Clarifying question must be non-empty and at most 300 characters.");
        }

        return new ClarifyingQuestion(trimmed);
    }

    public override string ToString() => Value;
}
