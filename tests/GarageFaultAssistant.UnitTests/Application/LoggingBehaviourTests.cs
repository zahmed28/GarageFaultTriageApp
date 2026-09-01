using GarageFaultAssistant.Api.Application.Common;
using GarageFaultAssistant.Api.Application.Common.Behaviours;
using GarageFaultAssistant.Api.Application.Common.DependencyInjection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GarageFaultAssistant.UnitTests.Application;

public class LoggingBehaviourTests
{
    private const string SecretDescription =
        "SECRET_PII_DESCRIPTION_TEXT_that_must_never_appear_in_logs";

    [Fact]
    public async Task Handle_logs_description_length_but_not_raw_text()
    {
        var logger = new ListLogger<LoggingBehaviour<TestDescriptionRequest, string>>();
        var behaviour = new LoggingBehaviour<TestDescriptionRequest, string>(logger);
        var request = new TestDescriptionRequest(SecretDescription);

        var response = await behaviour.Handle(
            request,
            (ct) => Task.FromResult("ok"),
            CancellationToken.None);

        Assert.Equal("ok", response);

        var allMessages = string.Join(Environment.NewLine, logger.Messages);
        Assert.DoesNotContain(SecretDescription, allMessages);
        Assert.Contains(SecretDescription.Length.ToString(), allMessages);
        Assert.Contains(nameof(TestDescriptionRequest), allMessages);
        Assert.Contains("successfully", allMessages, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_on_failure_logs_length_but_not_raw_text()
    {
        var logger = new ListLogger<LoggingBehaviour<TestDescriptionRequest, string>>();
        var behaviour = new LoggingBehaviour<TestDescriptionRequest, string>(logger);
        var request = new TestDescriptionRequest(SecretDescription);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behaviour.Handle(
                request,
                (ct) => throw new InvalidOperationException("boom"),
                CancellationToken.None));

        var allMessages = string.Join(Environment.NewLine, logger.Messages);
        Assert.DoesNotContain(SecretDescription, allMessages);
        Assert.Contains(SecretDescription.Length.ToString(), allMessages);
        Assert.Contains("Failed", allMessages);
    }

    [Fact]
    public void AddApplication_registers_without_throwing()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();

        using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();
        Assert.NotNull(sender);
    }

    private sealed record TestDescriptionRequest(string Description)
        : IRequest<string>, IHasDescription;

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
