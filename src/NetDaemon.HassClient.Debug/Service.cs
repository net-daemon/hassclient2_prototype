
namespace NetDaemon.HassClient.Debug;
internal class DebugService : BackgroundService
{
    private const int _timeoutInSeconds = 5;

    private readonly IHostApplicationLifetime _hostLifetime;
    private readonly IHomeAssistantRunner _homeAssistantRunner;
    private IHomeAssistantConnection? _connection;

    private readonly HomeAssistantSettings _haSettings;

    private readonly ILogger<DebugService> _logger;

    private CancellationToken? _cancelToken;
    public DebugService(
        IHostApplicationLifetime hostLifetime,
        IHomeAssistantRunner homeAssistantRunner,
        IOptions<HomeAssistantSettings> settings,
        ILogger<DebugService> logger)
    {
        _haSettings = settings.Value;
        _hostLifetime = hostLifetime;
        _homeAssistantRunner = homeAssistantRunner;
        _logger = logger;

        homeAssistantRunner.OnConnect.Subscribe(async (s) => await OnHomeAssistantClientConnected(s).ConfigureAwait(false));
        homeAssistantRunner.OnDisconnect.Subscribe(s => OnHomeAssistantClientDisconnected(s));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _cancelToken = stoppingToken;
        await _homeAssistantRunner.RunAsync(
                    _haSettings.Host,
                    _haSettings.Port,
                    _haSettings.Ssl,
                    _haSettings.Token,
                    TimeSpan.FromSeconds(_timeoutInSeconds),
                    stoppingToken).ConfigureAwait(false);

        // Stop application if this is exited
        _hostLifetime.StopApplication();
    }

    private async Task OnHomeAssistantClientConnected(IHomeAssistantConnection connection)
    {
        _logger.LogInformation("HassClient connected and processing events");
        connection.OnHomeAssistantEvent.Subscribe(s => HandleEvent(s));
        var services = await connection.GetServicesAsync(_cancelToken ?? CancellationToken.None);
        // Example set state and create new entity
        // var state = await connection.PostApiCall<HassState>($"states/{HttpUtility.UrlEncode("light.test")}", _cancelToken ?? CancellationToken.None, new { state = "on", attributes = new { myattribute = "hello" } }).ConfigureAwait(false);
        //_logger.LogInformation("Added entity: {entity}", state);
    }
    private void OnHomeAssistantClientDisconnected(DisconnectReason reason)
    {
        _logger.LogInformation("HassClient disconnected cause of {reason}, connect retry in {timeout} seconds", _timeoutInSeconds, reason);
        // Here you would typically cancel and dispose any functions  
        // using the connection
        if (_connection is not null)
        {
            _connection = null;
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