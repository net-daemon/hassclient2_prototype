namespace NetDaemon.HassClient.Tests.Integration;

public class HomeAssistantServiceFixture : IAsyncLifetime
{
    public HomeAssistantMock? HaMock { get; set; }
    public async Task DisposeAsync()
    {
        if (HaMock is not null)
            await HaMock.DisposeAsync().ConfigureAwait(false);
    }

    public Task InitializeAsync()
    {
        HaMock = new HomeAssistantMock();
        return Task.CompletedTask;
    }
}

public class IntegrationTests : IClassFixture<HomeAssistantServiceFixture>
{
    public CancellationTokenSource _tokenSource = new(TestSettings.DefaultTimeout);
    public IntegrationTests(HomeAssistantServiceFixture fixture)
    {
        HaFixture = fixture;
    }

    private HomeAssistantServiceFixture HaFixture { get; }


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
                null,
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

    private record TestContext : IAsyncDisposable
    {
        public Mock<ILogger<HomeAssistantClient>> HomeAssistantLogger { get; init; } = new();
        public Mock<ILogger<IWebSocketClientTransportPipeline>> TransportPipelineLogger { get; init; } = new();
        public Mock<ILogger<IHomeAssistantConnection>> HomeAssistantConnectionLogger { get; init; } = new();
        public IHomeAssistantConnection HomeAssistantConnction { get; init; } = new Mock<IHomeAssistantConnection>().Object;
        public async ValueTask DisposeAsync()
        {
            await HomeAssistantConnction.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task<TestContext> GetConnectedClientContext(HomeAssistantSettings? haSettings = null)
    {
        var mock = HaFixture?.HaMock ?? throw new ApplicationException("Unexpected for the mock server to be null");

        var loggerClient = new Mock<ILogger<HomeAssistantClient>>();
        var loggerTransport = new Mock<ILogger<IWebSocketClientTransportPipeline>>();
        var loggerConnection = new Mock<ILogger<IHomeAssistantConnection>>();

        var settings = haSettings ?? new HomeAssistantSettings()
        {
            Host = "127.0.0.1",
            Port = mock?.ServerPort ?? 0,
            Ssl = false,
            Token = "ABCDEFGHIJKLMNOPQ"
        };

        IOptions<HomeAssistantSettings> appSettingsOptions = Options.Create(settings);

        var client = new HomeAssistantClient(
            loggerClient.Object,
            new WebSocketClientFactory(),
            new WebSocketClientTransportPipelineFactory(loggerTransport.Object),
            new HomeAssistantConnectionFactory(
                loggerConnection.Object,
                new HomeAssistantApiManager(
                    appSettingsOptions,
                    (mock?.HomeAssistantHost.Services.GetRequiredService<IHttpClientFactory>() ?? throw new NullReferenceException())
                    .CreateClient()
                )
            )

        );
        var connection = await client.ConnectAsync(
            settings.Host,
            settings.Port,
            settings.Ssl,
            settings.Token,
            _tokenSource.Token
        ).ConfigureAwait(false);

        return new TestContext
        {
            HomeAssistantLogger = loggerClient,
            TransportPipelineLogger = loggerTransport,
            HomeAssistantConnectionLogger = loggerConnection,
            HomeAssistantConnction = connection
        };
    }
}