using FluentValidation;
using GarageFaultAssistant.Api.Application.Common.Behaviours;
using MediatR;

namespace GarageFaultAssistant.UnitTests.Application;

public class ValidationBehaviourTests
{
    [Fact]
    public async Task Handle_with_invalid_request_throws_ValidationException()
    {
        var validators = new IValidator<TestRequest>[] { new TestRequestValidator() };
        var behaviour = new ValidationBehaviour<TestRequest, string>(validators);
        var request = new TestRequest(string.Empty);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            behaviour.Handle(
                request,
                (ct) => Task.FromResult("should-not-run"),
                CancellationToken.None));

        Assert.Contains(exception.Errors, e => e.PropertyName == nameof(TestRequest.Name));
    }

    [Fact]
    public async Task Handle_with_valid_request_calls_next()
    {
        var validators = new IValidator<TestRequest>[] { new TestRequestValidator() };
        var behaviour = new ValidationBehaviour<TestRequest, string>(validators);
        var request = new TestRequest("valid");

        var response = await behaviour.Handle(
            request,
            (ct) => Task.FromResult("ok"),
            CancellationToken.None);

        Assert.Equal("ok", response);
    }

    [Fact]
    public async Task Handle_with_no_validators_calls_next()
    {
        var behaviour = new ValidationBehaviour<TestRequest, string>(
            Array.Empty<IValidator<TestRequest>>());
        var request = new TestRequest(string.Empty);

        var response = await behaviour.Handle(
            request,
            (ct) => Task.FromResult("ok"),
            CancellationToken.None);

        Assert.Equal("ok", response);
    }

    private sealed record TestRequest(string Name) : IRequest<string>;

    private sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
