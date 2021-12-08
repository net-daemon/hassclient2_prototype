namespace NetDaemon.HassClient.Tests.Integration;

public class WebsocketIntegrationTests : IntegrationTestBase
{
    public WebsocketIntegrationTests(HomeAssistantServiceFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task TestSuccessfulConnectShouldReturnConnection()
    {
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        ctx.HomeAssistantConnction.Should().NotBeNull();
    }

    [Fact]
    public async Task TestGetServicesShouldReturnRawJsonElement()
    {
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        var services = await ctx.HomeAssistantConnction
            .GetServicesAsync(_tokenSource.Token)
            .ConfigureAwait(false);

        services.Should().NotBeNull();
    }

    [Fact]
    public async Task TestCallServiceShouldSucceed()
    {
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        await ctx.HomeAssistantConnction
            .CallServiceAsync(
                "domain",
                "service",
                null,
                new()
                {
                    EntityIds = new[] { "light.test" }
                },
                _tokenSource.Token)
            .ConfigureAwait(false);
    }

    [Fact]
    public async Task TestGetDevicesShouldHaveCorrectCounts()
    {
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        var services = await ctx.HomeAssistantConnction
            .GetDevicesAsync(_tokenSource.Token)
            .ConfigureAwait(false);

        services.Should().HaveCount(2);
    }

    [Fact]
    public async Task TestGetStatesShouldHaveCorrectCounts()
    {
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        var states = await ctx.HomeAssistantConnction
            .GetStatesAsync(_tokenSource.Token)
            .ConfigureAwait(false);

        states.Should().HaveCount(19);
    }

    [Fact]
    public async Task TestUnothorizedShouldThrowCorrectException()
    {
        var mock = HaFixture?.HaMock ?? throw new ApplicationException("Unexpected for the mock server to be null");

        var settings = new HomeAssistantSettings
        {
            Host = "127.0.0.1",
            Port = mock?.ServerPort ?? 0,
            Ssl = false,
            Token = "wrong token"
        };
        await Assert.ThrowsAsync<HomeAssistantConnectionException>(
            async () => await GetConnectedClientContext(settings).ConfigureAwait(false));
    }

    [Fact]
    public async Task TestWrongHostShouldThrowCorrectException()
    {
        var mock = HaFixture?.HaMock ?? throw new ApplicationException("Unexpected for the mock server to be null");

        var settings = new HomeAssistantSettings
        {
            Host = "127.0.0.2",
            Port = mock?.ServerPort ?? 0,
            Ssl = false,
            Token = "token does not matter"
        };
        await Assert.ThrowsAsync<WebSocketException>(async () => await GetConnectedClientContext(settings).ConfigureAwait(false));
    }

    [Fact]
    public async Task TestGetEntitesShouldHaveCorrectCounts()
    {
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        var entites = await ctx.HomeAssistantConnction
            .GetEntitiesAsync(_tokenSource.Token)
            .ConfigureAwait(false);

        entites.Should().HaveCount(2);
    }

    [Fact]
    public async Task TestGetConfigShouldReturnCorrectInformation()
    {
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        var config = await ctx.HomeAssistantConnction
            .GetConfigAsync(_tokenSource.Token)
            .ConfigureAwait(false);

        config
            .Should()
            .NotBeNull();
        config?.Latitude
            .Should()
            .Be(63.1394549f);
    }

    [Fact]
    public async Task TestPing()
    {
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        var config = await ctx.HomeAssistantConnction
            .PingAsync(TimeSpan.FromMilliseconds(TestSettings.DefaultTimeout), _tokenSource.Token)
            .ConfigureAwait(false);

    }

    [Fact]
    public async Task TestGetAreasShouldHaveCorrectCounts()
    {
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        var services = await ctx.HomeAssistantConnction
            .GetAreasAsync(_tokenSource.Token)
            .ConfigureAwait(false);

        services.Should().HaveCount(3);
    }

    [Fact]
    public async Task TestErrorReturnShouldThrowExecption()
    {
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        await Assert.ThrowsAsync<ApplicationException>(async () => await ctx.HomeAssistantConnction
            .SendCommandAndReturnResponseAsync<SimpleCommand, object?>(
                new SimpleCommand("fake_return_error"),
                _tokenSource.Token
            )
            .ConfigureAwait(false));

    }

    private record AttributeTest
    {
        [JsonPropertyName("battery_level")]
        public int BatteryLevel { get; set; }
    }

    [Fact]
    public async Task TestSubscribeAndGetEvent()
    {
        using CancellationTokenSource tokenSource = new(TestSettings.DefaultTimeout + 1000);
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        var subscribeTask = ctx.HomeAssistantConnction
            .OnHomeAssistantEvent
            .Timeout(TimeSpan.FromMilliseconds(TestSettings.DefaultTimeout), Observable.Return(default(HassEvent?)))
            .FirstAsync()
            .ToTask();

        var processEventsTask = ctx.HomeAssistantConnction
            .ProcessHomeAssistantEventsAsync(tokenSource.Token);

        var haEvent = await subscribeTask.ConfigureAwait(false);

        haEvent.Should().NotBeNull();
        haEvent?
            .EventType
            .Should()
                .BeEquivalentTo("state_changed");

        // We test the state changed 
        var changedEvent = haEvent?.ToStateChangedEvent();
        changedEvent?.EntityId
            .Should()
            .BeEquivalentTo("binary_sensor.vardagsrum_pir");

        var attr = changedEvent?.NewState?.AttributesAs<AttributeTest>();
        attr?
            .BatteryLevel
            .Should()
            .Be(100);

        changedEvent?.NewState?.Attributes?.FirstOrDefault(n => n.Key == "battery_level")
            .Should()
            .NotBeNull();
    }

    [Fact]
    public async Task TestGetServiceEvent()
    {
        using CancellationTokenSource tokenSource = new(TestSettings.DefaultTimeout + 1000);
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        var subscribeTask = ctx.HomeAssistantConnction
            .OnHomeAssistantEvent
            .Where(n => n.EventType == "call_service")
            .Timeout(TimeSpan.FromMilliseconds(TestSettings.DefaultTimeout), Observable.Return(default(HassEvent?)))
            .FirstAsync()
            .ToTask();

        var processEventsTask = ctx.HomeAssistantConnction
            .ProcessHomeAssistantEventsAsync(tokenSource.Token);

        await ctx.HomeAssistantConnction
           .SendCommandAndReturnResponseAsync<SimpleCommand, object?>(
               new SimpleCommand("fake_service_event"),
               _tokenSource.Token
           )
           .ConfigureAwait(false);

        var haEvent = await subscribeTask.ConfigureAwait(false);

        haEvent.Should().NotBeNull();
        haEvent?
            .EventType
            .Should()
                .BeEquivalentTo("call_service");
        // Test the convertion to service event
        var stateChangedEvent = haEvent?.ToCallServiceEvent();
        stateChangedEvent?.Domain
            .Should()
            .BeEquivalentTo("light");
    }

}
