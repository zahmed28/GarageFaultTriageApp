namespace GarageFaultAssistant.Api.Domain;

public sealed class CustomerConcern
{
    public const int MaxLength = 500;

    private CustomerConcern(string value) => Value = value;

    public string Value { get; }

    public static CustomerConcern Create(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxLength)
        {
            throw new FaultAnalysisRejectedException(
                "Customer concern must be non-empty and at most 500 characters.");
        }

        return new CustomerConcern(trimmed);
    }

    public override string ToString() => Value;
}
