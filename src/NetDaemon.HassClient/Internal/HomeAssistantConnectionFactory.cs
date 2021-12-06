
using Netdaemon.Client.Common;
using NetDaemon.Client.Common.Net;

namespace NetDaemon.Client.Internal;

public class HomeAssistantConnectionFactory : IHomeAssistantConnectionFactory
{
    private readonly ILogger<IHomeAssistantConnection> _logger;

    public HomeAssistantConnectionFactory(
        ILogger<IHomeAssistantConnection> logger
    )
    {
        _logger = logger;
    }

    public IHomeAssistantConnection New(IWebSocketClientTransportPipeline transportPipeline)
    {
        return new HomeAssistantConnection(_logger, transportPipeline);
    }
}