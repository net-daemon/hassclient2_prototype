namespace NetDaemon.Client.Common.Net;

/// <summary>
///     Factory for Client Websocket. Implement to use for mockups
/// </summary>
public interface IWebSocketClientFactory
{
    IWebSocketClient New();
}
