namespace NetDaemon.Client.Internal;

internal class HomeAssistantRunner : IHomeAssistantRunner
{
    private readonly IHomeAssistantClient _client;

    // The internal token source will make sure we 
    // always cancel operations on dispose
    private readonly CancellationTokenSource _internalTokenSource = new();
    private Task? _runTask;
    public HomeAssistantRunner(
        IHomeAssistantClient client,
        ILogger<IHomeAssistantRunner> logger
    )
    {
        _client = client;
        Logger = logger;
    }
    private readonly Subject<IHomeAssistantConnection> _onConnectSubject = new();
    public IObservable<IHomeAssistantConnection> OnConnect => _onConnectSubject;

    private readonly Subject<DisconnectReason> _onDisconnectSubject = new();
    public IObservable<DisconnectReason> OnDisconnect => _onDisconnectSubject;

    public ILogger<IHomeAssistantRunner> Logger { get; }

    public Task RunAsync(string host, int port, bool ssl, string token, TimeSpan timeout, CancellationToken cancelToken)
    {
        _runTask = InternalRunAsync(host, port, ssl, token, timeout, cancelToken);
        return _runTask;
    }

    private async Task InternalRunAsync(string host, int port, bool ssl, string token, TimeSpan timeout, CancellationToken cancelToken)
    {
        // We create a 
        var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(_internalTokenSource.Token, cancelToken);
        bool isRetry = false;
        while (!combinedToken.IsCancellationRequested)
        {
            if (isRetry)
            {
                Logger.LogDebug("Client disconnected, retrying in {seconds} seconds...", timeout.TotalSeconds);
                // This is a retry
                await Task.Delay(timeout, combinedToken.Token).ConfigureAwait(false);
            }
            try
            {
                var connection = await _client.ConnectAsync(host, port, ssl, token, combinedToken.Token).ConfigureAwait(false);
                // Start the event processing before publish the connection
                var eventsTask = connection.ProcessHomeAssistantEventsAsync(combinedToken.Token);
                _onConnectSubject.OnNext(connection);
                await eventsTask.ConfigureAwait(false);
            }
            catch (HomeAssistantConnectionException de)
            {
                switch (de.Reason)
                {
                    case DisconnectReason.Unauthorized:
                        Logger.LogDebug("User token unauthorized! Will not retry connecting...");
                        _onDisconnectSubject.OnNext(DisconnectReason.Unauthorized);
                        return;
                    case DisconnectReason.NotReady:
                        Logger.LogDebug("Home Assistant is not ready yet!");
                        _onDisconnectSubject.OnNext(DisconnectReason.NotReady);
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogDebug("Run cancelled");
                if (_internalTokenSource.IsCancellationRequested)
                {
                    // We have internal cancellation due to dispose
                    // just return without any further due
                    return;
                }
                if (cancelToken.IsCancellationRequested)
                    _onDisconnectSubject.OnNext(DisconnectReason.Client);
                else
                    _onDisconnectSubject.OnNext(DisconnectReason.Remote);
            }
            catch (Exception e)
            {
                Logger.LogError("Error running HassClient", e);
                _onDisconnectSubject.OnNext(DisconnectReason.Error);
                throw;
            }
            isRetry = true;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _internalTokenSource.Cancel();
        if (_runTask?.IsCompleted == false)
        {
            try
            {
                await Task.WhenAny(
                    _runTask,
                    Task.Delay(5000)
                ).ConfigureAwait(false);
            }
            catch
            {
                // Ignore errors
            }
        }
        _onConnectSubject.Dispose();
        _onDisconnectSubject.Dispose();
        _internalTokenSource.Dispose();
    }
}