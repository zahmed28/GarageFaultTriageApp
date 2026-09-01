namespace GarageFaultAssistant.Api.Domain;

public sealed class FaultDescription
{
    public const int MinLength = 10;
    public const int MaxLength = 2000;

    private FaultDescription(string value) => Value = value;

    public string Value { get; }

    public static FaultDescription Create(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length < MinLength || trimmed.Length > MaxLength)
        {
            throw new FaultAnalysisRejectedException(
                "Fault description must be between 10 and 2000 characters.");
        }

        return new FaultDescription(trimmed);
    }

    public override string ToString() => Value;
}
