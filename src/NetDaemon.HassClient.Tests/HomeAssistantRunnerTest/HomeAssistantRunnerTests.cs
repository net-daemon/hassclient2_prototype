using NetDaemon.Client.Internal;
using NNetDaemon.HassClient.Tests.HomeAssistantRunnerTest;

namespace NetDaemon.HassClient.Tests.HomeAssistantRunnerTest;


public class HomeAssistantRunnerTests
{
    private readonly HomeAssistantClientMock ClientMock = new();

    private readonly Mock<ILogger<IHomeAssistantRunner>> LogMock = new();

    private HomeAssistantRunner DefaultRunner { get; }

    public HomeAssistantRunnerTests()
    {

        DefaultRunner = new(ClientMock.Object, LogMock.Object);
    }
    [Fact]
    public async Task TestSuccessfullShouldPostConnection()
    {
        using var cancelSource = new CancellationTokenSource();

        var connectionTask = DefaultRunner.OnConnect
            .Timeout(TimeSpan.FromMilliseconds(TestSettings.DefaultTimeout), Observable.Return(default(IHomeAssistantConnection?)))
            .FirstAsync()
            .ToTask();

        var runnerTask = DefaultRunner.RunAsync("host", 0, false, "token", TimeSpan.FromMilliseconds(100), cancelSource.Token);

        var connection = await connectionTask.ConfigureAwait(false);
        try
        {
            cancelSource.Cancel();
            await runnerTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) { } // ignore cancel error
        connection.Should().NotBeNull();
    }

    [Fact]
    public async Task TestUnSuccessfullConnectionShouldPostCorrectDisconnectError()
    {
        using var cancelSource = new CancellationTokenSource();
        ClientMock.Setup(n =>
            n.ConnectAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            )
        ).Throws(new WebSocketException("What ever"));

        var disconnectionTask = DefaultRunner.OnDisconnect
            .Timeout(TimeSpan.FromMilliseconds(TestSettings.DefaultTimeout), Observable.Return(default(DisconnectReason)))
            .FirstAsync()
            .ToTask();

        var runnerTask = DefaultRunner.RunAsync("host", 0, false, "token", TimeSpan.FromMilliseconds(100), cancelSource.Token);

        var reason = await disconnectionTask.ConfigureAwait(false);
        try
        {
            cancelSource.Cancel();
            await runnerTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) { } // ignore cancel error
        reason.Should().Be(DisconnectReason.Error);
    }

    [Fact]
    public async Task TestNotReadyConnectionShouldPostCorrectDisconnectError()
    {
        using var cancelSource = new CancellationTokenSource();
        ClientMock.Setup(n =>
            n.ConnectAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            )
        ).Throws(new HomeAssistantConnectionException(DisconnectReason.NotReady));

        var disconnectionTask = DefaultRunner.OnDisconnect
            .Timeout(TimeSpan.FromMilliseconds(TestSettings.DefaultTimeout), Observable.Return(default(DisconnectReason)))
            .FirstAsync()
            .ToTask();

        var runnerTask = DefaultRunner.RunAsync("host", 0, false, "token", TimeSpan.FromMilliseconds(100), cancelSource.Token);

        var reason = await disconnectionTask.ConfigureAwait(false);
        try
        {
            cancelSource.Cancel();
            await runnerTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) { } // ignore cancel error
        reason.Should().Be(DisconnectReason.NotReady);
    }

    [Fact]
    public async Task TestNotAutorizedConnectionShouldPostCorrectDisconnectError()
    {
        using var cancelSource = new CancellationTokenSource();
        ClientMock.Setup(n =>
            n.ConnectAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            )
        ).Throws(new HomeAssistantConnectionException(DisconnectReason.Unauthorized));

        var disconnectionTask = DefaultRunner.OnDisconnect
            .Timeout(TimeSpan.FromMilliseconds(TestSettings.DefaultTimeout), Observable.Return(default(DisconnectReason)))
            .FirstAsync()
            .ToTask();

        var runnerTask = DefaultRunner.RunAsync("host", 0, false, "token", TimeSpan.FromMilliseconds(100), cancelSource.Token);

        var reason = await disconnectionTask.ConfigureAwait(false);
        try
        {
            cancelSource.Cancel();
            await runnerTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) { } // ignore cancel error
        reason.Should().Be(DisconnectReason.Unauthorized);
    }

    [Fact]
    public async Task TestClientDisconnectShouldPostCorrectDisconnectError()
    {
        using var cancelSource = new CancellationTokenSource();

        var disconnectionTask = DefaultRunner.OnDisconnect
            .Timeout(TimeSpan.FromMilliseconds(TestSettings.DefaultTimeout), Observable.Return(default(DisconnectReason)))
            .FirstAsync()
            .ToTask();

        var runnerTask = DefaultRunner.RunAsync("host", 0, false, "token", TimeSpan.FromMilliseconds(100), cancelSource.Token);

        // await DefaultRunner.DisposeAsync().ConfigureAwait(false);
        cancelSource.Cancel();
        var reason = await disconnectionTask.ConfigureAwait(false);
        try
        {
            await runnerTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) { } // ignore cancel error
        reason.Should().Be(DisconnectReason.Client);
    }

    [Fact]
    public async Task TestRemoteDisconnectShouldPostCorrectDisconnectError()
    {
        using var cancelSource = new CancellationTokenSource();
        ClientMock.Setup(n =>
            n.ConnectAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            )
        ).Throws<OperationCanceledException>();

        var disconnectionTask = DefaultRunner.OnDisconnect
            .Timeout(TimeSpan.FromMilliseconds(TestSettings.DefaultTimeout), Observable.Return(default(DisconnectReason)))
            .FirstAsync()
            .ToTask();

        var runnerTask = DefaultRunner.RunAsync("host", 0, false, "token", TimeSpan.FromMilliseconds(100), cancelSource.Token);

        var reason = await disconnectionTask.ConfigureAwait(false);
        try
        {
            cancelSource.Cancel();
            await runnerTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) { } // ignore cancel error
        reason.Should().Be(DisconnectReason.Remote);
    }

}