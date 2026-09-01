using FluentValidation.TestHelper;
using GarageFaultAssistant.Api.Application.AnalyseFault;

namespace GarageFaultAssistant.UnitTests.Application;

public class AnalyseFaultValidatorTests
{
    private readonly AnalyseFaultValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Description_null_empty_or_whitespace_is_invalid(string? description)
    {
        var result = _validator.TestValidate(new AnalyseFaultCommand(description!));

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Description_9_chars_after_trim_is_invalid()
    {
        var result = _validator.TestValidate(new AnalyseFaultCommand("123456789"));

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Description_10_chars_after_trim_is_valid()
    {
        var result = _validator.TestValidate(new AnalyseFaultCommand("1234567890"));

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Description_2000_chars_is_valid()
    {
        var result = _validator.TestValidate(new AnalyseFaultCommand(new string('a', 2000)));

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Description_2001_chars_is_invalid()
    {
        var result = _validator.TestValidate(new AnalyseFaultCommand(new string('a', 2001)));

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Description_with_padding_whitespace_and_trimmed_length_10_is_valid()
    {
        var result = _validator.TestValidate(new AnalyseFaultCommand("  1234567890  "));

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Description_with_padding_whitespace_and_trimmed_length_9_is_invalid()
    {
        var result = _validator.TestValidate(new AnalyseFaultCommand("  123456789  "));

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}
