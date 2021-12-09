using NetDaemon.HassClient.Tests.HomeAssistantClientTest;
using NetDaemon.HassClient.Tests.Net;

namespace NNetDaemon.HassClient.Tests.HomeAssistantRunnerTest;

internal class HomeAssistantClientMock : Mock<IHomeAssistantClient>
{
    private readonly HomeAssistantConnectionMock HaConnectionMock = new();

    public HomeAssistantClientMock()
    {
        // Return a mock connection as default
        Setup(n =>
            n.ConnectAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            )
        ).Returns(
            (string host, int port, bool ssl, string token, CancellationToken cancellationToken) =>
            {
                return Task.FromResult(HaConnectionMock.Object);
            }
        );

        HaConnectionMock.Setup(n =>
            n.ProcessHomeAssistantEventsAsync(It.IsAny<CancellationToken>())
        ).Returns(
            async (CancellationToken cancelToken) =>
            {
                await Task.Delay(TestSettings.DefaultTimeout, cancelToken).ConfigureAwait(false);
            }
        );
    }

}
