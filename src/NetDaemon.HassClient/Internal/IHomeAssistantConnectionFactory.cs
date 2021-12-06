using Netdaemon.Client.Common;
using NetDaemon.Client.Common.Net;

namespace NetDaemon.Client.Internal;

internal interface IHomeAssistantConnectionFactory
{
    IHomeAssistantConnection New(IWebSocketClientTransportPipeline transportPipeline);
}