namespace NetDaemon.Client.Internal;

internal class HomeAssistantConnectionFactory : IHomeAssistantConnectionFactory
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