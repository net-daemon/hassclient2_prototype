
namespace NetDaemon.HassClient.Debug;
internal class DebugService : BackgroundService
{
    private readonly IHostApplicationLifetime _hostLifetime;
    private readonly IHomeAssistantClient _homeAssistantClient;

    private readonly HomeAssistantSettings _haSettings;

    private readonly ILogger<DebugService> _logger;
    public DebugService(
        IHostApplicationLifetime hostLifetime,
        IHomeAssistantClient homeAssistantClient,
        IOptions<HomeAssistantSettings> settings,
        ILogger<DebugService> logger)
    {
        _haSettings = settings.Value;
        _hostLifetime = hostLifetime;
        _homeAssistantClient = homeAssistantClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var homeAssistantConnection = await _homeAssistantClient.ConnectAsync(
                    _haSettings.Host,
                    _haSettings.Port,
                    _haSettings.Ssl,
                    _haSettings.Token,
                    stoppingToken).ConfigureAwait(false);
        homeAssistantConnection.HassEvents.Subscribe(s => HandleEvent(s));
        try
        {
            await homeAssistantConnection.ProcessHomeAssistantEventsAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            // Normal operation
            _logger.LogInformation("Service exiting due to canceled work!");
            _hostLifetime.StopApplication();
        }
    }

    private void HandleEvent(HassEvent hassEvent)
    {
        _logger.LogDebug("New event ({eventType})", hassEvent.EventType);
        switch (hassEvent.EventType)
        {
            case "state_changed":
                var state = hassEvent.ToStateChangedEvent();
                _logger.LogInformation("state changed: {state}", state);
                break;
        }
    }
}