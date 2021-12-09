using NetDaemon.HassClient.Tests.Net;

namespace NetDaemon.HassClient.Tests.HomeAssistantClientTest;

public class HomeAssistantClientTests
{
    private readonly TransportPipelineMock Pipeline = new();
    private readonly WebSocketClientMock WsMock = new();
    private readonly HomeAssistantConnectionMock HaConnectionMock = new();

    /// <summary>
    ///     Return a mocked Home Assistant Client
    /// </summary>
    internal HomeAssistantClient GetDefaultHomeAssistantClient()
    {
        var connFactoryMock = new Mock<IHomeAssistantConnectionFactory>();
        var loggerMock = new Mock<ILogger<HomeAssistantClient>>();
        var wsClientFactoryMock = new Mock<IWebSocketClientFactory>();
        var transportPipelingFactoryMock = new Mock<IWebSocketClientTransportPipelineFactory>();

        wsClientFactoryMock.Setup(n => n.New()).Returns(WsMock.Object);
        transportPipelingFactoryMock.Setup(n => n.New(It.IsAny<IWebSocketClient>())).Returns(Pipeline.Object);
        connFactoryMock.Setup(n =>
            n.New(It.IsAny<IWebSocketClientTransportPipeline>())).Returns(HaConnectionMock.Object);
        return new HomeAssistantClient(
                    loggerMock.Object,
                    wsClientFactoryMock.Object,
                    transportPipelingFactoryMock.Object,
                    connFactoryMock.Object);
    }

    [Fact]
    public async Task TestConnectWithHomeAShouldReturnConnection()
    {
        var client = GetDefaultConnectOkHomeAssistantClient();

        var connection = await client.ConnectAsync("host", 1, true, "token", CancellationToken.None).ConfigureAwait(false);

        connection!.Should().NotBeNull();
    }

    [Fact]
    public async Task TestConnectWithHomeAssistantNotReadyShouldThrowException()
    {
        var client = GetDefaultAutorizedHomeAssistantClient();

        HaConnectionMock.AddConfigResponseMessage(
            new()
            {
                State = "ANY_STATE_BUT_RUNNING"
            }
        );

        await Assert.ThrowsAsync<HomeAssistantConnectionException>(async () => await client.ConnectAsync("host", 1, true, "token", CancellationToken.None).ConfigureAwait(false));
    }

    [Fact]
    public void TestInstanceNewConnectionOnClosedWebsocketThrowsExceptionShouldThrowException()
    {
        Pipeline.SetupGet(
            n => n.WebSocketState
        ).Returns(WebSocketState.Closed);
        var loggerMock = new Mock<ILogger<IHomeAssistantConnection>>();
        Assert.Throws<ApplicationException>(() =>
          _ = new HomeAssistantConnection(
          loggerMock.Object,
          Pipeline.Object,
          new Mock<IHomeAssistantApiManager>().Object));
    }

    /// <summary>
    ///     Return a pre-authenticated OK HomeAssistantClient
    /// </summary>
    internal HomeAssistantClient GetDefaultAutorizedHomeAssistantClient()
    {
        // First add the autorization responses from pipeline
        Pipeline.AddResponse(
            new HassMessage
            {
                Type = "auth_required"
            }
        );
        Pipeline.AddResponse(
            new HassMessage
            {
                Type = "auth_ok"
            }
        );
        return GetDefaultHomeAssistantClient();
    }

    /// <summary>
    ///     Return a pre authenticated and running state 
    ///     HomeAssistantClient
    /// </summary>
    internal HomeAssistantClient GetDefaultConnectOkHomeAssistantClient()
    {
        // For a successfull connection we need success on autorization
        // and success on getting a config message that has state="RUNNING"

        // First add the autorization responses from pipeline
        Pipeline.AddResponse(
            new HassMessage
            {
                Type = "auth_required"
            }
        );
        Pipeline.AddResponse(
            new HassMessage
            {
                Type = "auth_ok"
            }
        );
        // The add the fake config state that says running
        HaConnectionMock.AddConfigResponseMessage(
            new()
            {
                State = "RUNNING"
            }
        );
        return GetDefaultHomeAssistantClient();

    }
}