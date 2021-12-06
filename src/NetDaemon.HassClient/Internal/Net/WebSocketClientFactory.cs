using NetDaemon.Client.Common.Net;

namespace NetDaemon.Client.Internal.Net;

internal class WebSocketClientFactory : IWebSocketClientFactory
{
    public IWebSocketClient New() => new WebSocketClientImpl();
}