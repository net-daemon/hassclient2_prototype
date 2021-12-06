using System.Net.WebSockets;
using Netdaemon.Client.Common;
using NetDaemon.Client.Common.Net;
using NetDaemon.Client.Internal.Extensions;
using NetDaemon.Client.Internal.HomeAssistant.Commands;
using NetDaemon.Client.Internal.Json;
using NetDaemon.Client.Common.HomeAssistant.Model;
using System.Diagnostics.CodeAnalysis;

namespace NetDaemon.Client.Internal;

internal class HomeAssistantConnection : IHomeAssistantConnection, IHomeAssistantHassMessages
{
    #region -- private declarations -

    private readonly ILogger<IHomeAssistantConnection> _logger;
    private readonly IWebSocketClientTransportPipeline _transportPipeline;
    private readonly CancellationTokenSource _internalCancelSource = new();

    private readonly Subject<HassMessage> _hassMessageSubject = new();
    private readonly Task _handleNewMessagesTask;

    internal int WaitForResultTimeout { get; set; } = 5000;

    private int _messageId = 1;

    #endregion

    public IObservable<HassEvent> OnHomeAssistantEvent => _hassMessageSubject.Where(n => n.Type == "event").Select(n => n.Event!);
    public IObservable<HassMessage> OnHassMessage => _hassMessageSubject;

    /// <summary>
    ///     Default constructor
    /// </summary>
    /// <param name="logger">A logger instance</param>
    /// <param name="pipeline">The pipline to use for websocket communication</param>
    public HomeAssistantConnection(
        ILogger<IHomeAssistantConnection> logger,
        IWebSocketClientTransportPipeline pipeline
    )
    {
        _transportPipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _logger = logger;

        if (_transportPipeline.WebSocketState != WebSocketState.Open)
            throw new ApplicationException($"Expected WebSocket state 'Open' got '{_transportPipeline.WebSocketState}'");

        _handleNewMessagesTask = Task.Factory.StartNew(async () => await HandleNewMessages().ConfigureAwait(false),
            TaskCreationOptions.LongRunning);
    }

    public async Task ProcessHomeAssistantEventsAsync(CancellationToken cancelToken)
    {
        var combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancelToken,
            _internalCancelSource.Token
        );
        // Start by subscribing to all events
        await SubscribeToAllHomeAssistantEvents(combinedTokenSource.Token).ConfigureAwait(false);
        await combinedTokenSource.Token.AsTask().ConfigureAwait(false);
    }

    private async Task SubscribeToAllHomeAssistantEvents(CancellationToken cancelToken)
    {
        _ = await SendCommandAndReturnResponseAsync<SubscribeEventCommand, object?>(new SubscribeEventCommand(), cancelToken).ConfigureAwait(false);
    }

    public Task SendCommandAsync<T>(T command, CancellationToken cancelToken) where T : CommandMessage
    {
        command.Id = Interlocked.Increment(ref _messageId);
        return _transportPipeline.SendMessageAsync<T>(command, cancelToken);
    }

    public async Task<TResult?> SendCommandAndReturnResponseAsync<T, TResult>(T command, CancellationToken cancelToken) where T : CommandMessage
    {
        command.Id = Interlocked.Increment(ref _messageId);

        var resultEvent = _hassMessageSubject
            .Where(n => n.Type == "result" && n.Id == command.Id)
            .Timeout(TimeSpan.FromMilliseconds(WaitForResultTimeout), Observable.Return(default(HassMessage?)))
            .FirstAsync()
            .ToTask();

        await _transportPipeline.SendMessageAsync<T>(command, cancelToken).ConfigureAwait(false);
        var result = await resultEvent.ConfigureAwait(false) ??
            throw new ApplicationException("Send command ({command.Type}) did not get response in timely fashion");

        if (!result?.Success ?? false)
            throw new ApplicationException($"Failed command ({command.Type}) error: {result?.Error}");

        if (result?.ResultElement is not null)
        {
            if (command.Type == "get_services")
                return (TResult?)result.ResultElement.Value.ToServicesResult();
            else
                return result.ResultElement.Value.ToObject<TResult>();

        }

        return default;
    }

    private async Task HandleNewMessages()
    {
        try
        {
            while (!_internalCancelSource.IsCancellationRequested)
            {
                var msg = await _transportPipeline.GetNextMessageAsync<HassMessage>(_internalCancelSource.Token).ConfigureAwait(false);
                _hassMessageSubject.OnNext(msg);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal case just exit
        }
        finally
        {
            _logger.LogTrace("Stop processing new messages");
            // make sure we always cancel any blocking operations
            if (!_internalCancelSource.IsCancellationRequested)
                _internalCancelSource.Cancel();
        }
    }
    private Task CloseAsync()
    {
        return _transportPipeline.CloseAsync();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await CloseAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogDebug("Failed to close HomeAssistantConnection", e);
        }

        if (!_internalCancelSource.IsCancellationRequested)
            _internalCancelSource.Cancel();

        // Gracefully wait for task or timeout
        await Task.WhenAny(
            _handleNewMessagesTask,
            Task.Delay(5000)
        ).ConfigureAwait(false);

        await _transportPipeline.DisposeAsync().ConfigureAwait(false);
        _internalCancelSource.Dispose();
    }
}