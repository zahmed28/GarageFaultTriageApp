namespace GarageFaultAssistant.Api.Domain;

public sealed class Symptom
{
    public const int MaxLength = 200;

    private Symptom(string value) => Value = value;

    public string Value { get; }

    public static Symptom Create(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxLength)
        {
            throw new FaultAnalysisRejectedException(
                "Symptom must be non-empty and at most 200 characters.");
        }

        return new Symptom(trimmed);
    }

    public override string ToString() => Value;
}
