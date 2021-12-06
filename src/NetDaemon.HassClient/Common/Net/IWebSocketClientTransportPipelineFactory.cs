namespace NetDaemon.Client.Common.Net;

public interface IWebSocketClientTransportPipelineFactory
{
    IWebSocketClientTransportPipeline New(IWebSocketClient webSocketClient);
}
