using FluentValidation;

namespace GarageFaultAssistant.Api.Application.AnalyseFault;

public sealed class AnalyseFaultValidator : AbstractValidator<AnalyseFaultCommand>
{
    public const int MinLength = 10;
    public const int MaxLength = 2000;

    public AnalyseFaultValidator()
    {
        RuleFor(x => x.Description)
            .Cascade(CascadeMode.Stop)
            .Must(d => !string.IsNullOrWhiteSpace(d))
            .WithMessage("Description is required.")
            .Must(d =>
            {
                var length = d.Trim().Length;
                return length >= MinLength && length <= MaxLength;
            })
            .WithMessage($"Description must be between {MinLength} and {MaxLength} characters.");
    }
}
