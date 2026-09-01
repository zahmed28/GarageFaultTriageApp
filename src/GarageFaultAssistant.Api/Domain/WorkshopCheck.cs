namespace GarageFaultAssistant.Api.Domain;

public sealed class WorkshopCheck
{
    public const int MaxLength = 300;

    private WorkshopCheck(string value) => Value = value;

    public string Value { get; }

    public static WorkshopCheck Create(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxLength)
        {
            throw new FaultAnalysisRejectedException(
                "Workshop check must be non-empty and at most 300 characters.");
        }

        return new WorkshopCheck(trimmed);
    }

    public override string ToString() => Value;
}
