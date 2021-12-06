using Netdaemon.Client.Common;
using NetDaemon.Client.Internal.Net;

namespace NetDaemon.Client.Internal;

internal interface IHomeAssistantConnectionFactory
{
    IHomeAssistantConnection New(IWebSocketClientTransportPipeline transportPipeline);
}