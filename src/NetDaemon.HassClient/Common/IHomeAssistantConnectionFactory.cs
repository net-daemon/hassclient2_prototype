using NetDaemon.Client.Common.Net;

namespace Netdaemon.Client.Common;

public interface IHomeAssistantConnectionFactory
{
    IHomeAssistantConnection New(IWebSocketClientTransportPipeline transportPipeline);
}