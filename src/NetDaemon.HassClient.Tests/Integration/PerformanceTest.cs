using System.Diagnostics;
using Xunit.Abstractions;

namespace NetDaemon.HassClient.Tests.Integration;

public class WebsocketPerformanceTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;

    public const int NumberOfEventsInPerformanceTest = 10000;
    public WebsocketPerformanceTests(HomeAssistantServiceFixture fixture, ITestOutputHelper output) : base(fixture)
    {
        _output = output;
    }
    [Fact]
    public async Task TestSuccessfulConnectShouldReturnConnection()
    {
        using var cts = new CancellationTokenSource();
        var sw = new Stopwatch();
        var counter = 0;

        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        var rawMessageSubscriber = (IHomeAssistantHassMessages)ctx.HomeAssistantConnction;

        _output.WriteLine("-- Starting performance tests --");
        // Set the bar at 10000 msgs/second
        cts.CancelAfter(NumberOfEventsInPerformanceTest / 10);
        rawMessageSubscriber.OnHassMessage
            .Subscribe(
                s =>
                {
                    if (counter == 0)
                        sw.Start();
                    counter++;
                    if (counter == NumberOfEventsInPerformanceTest)
                    {
                        cts.Cancel();
                    }
                },
                cts.Token
                );
        await ctx.HomeAssistantConnction.SendCommandAsync(new SimpleCommand("fake_performance_test"), _tokenSource.Token).ConfigureAwait(false);
        try
        {
            await cts.Token.AsTask().ConfigureAwait(false);
        }
        catch (OperationCanceledException) { } //expected so ignore

        if (counter == NumberOfEventsInPerformanceTest)
        {
            sw.Stop();
            var totalExecutionTime = sw.Elapsed.TotalMilliseconds;
            _output.WriteLine($"-- Meassured performance was {(NumberOfEventsInPerformanceTest * 1000 / totalExecutionTime):0} messages/second --");
        }

        counter
            .Should()
            .Be(NumberOfEventsInPerformanceTest);
    }
}