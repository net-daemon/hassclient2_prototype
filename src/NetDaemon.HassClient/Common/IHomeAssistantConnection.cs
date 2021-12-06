using NetDaemon.Client.Common.HomeAssistant.Model;
using NetDaemon.Client.Internal.HomeAssistant.Commands;

namespace Netdaemon.Client.Common;

public interface IHomeAssistantConnection : IAsyncDisposable
{
    /// <summary>
    ///     Allows subsription on all events
    /// </summary>
    /// <remark>
    ///     This requires that "ProcessHomeAssistantEvents" task is running
    /// </remark>
    IObservable<HassEvent> HassEvents { get; }

    /// <summary>
    ///     Sends a command message to Home Assistant without handling the result
    /// </summary>
    /// <param name="command">Command message to send</param>
    /// <param name="cancelToken">token to cancel operation</param>
    /// <typeparam name="T">Type of command</typeparam>
    Task SendCommandAsync<T>(T command, CancellationToken cancelToken) where T : CommandMessage;

    /// <summary>
    ///     Sends a command message to Home Assistant without handling the result
    /// </summary>
    /// <param name="command">Command message to send</param>
    /// <param name="cancelToken">token to cancel operation</param>
    /// <typeparam name="T">Type of command</typeparam>
    /// <typeparam name="TResult">The result of the command</typeparam>
    Task<TResult?> SendCommandAndReturnResponseAsync<T, TResult>(T command, CancellationToken cancelToken) where T : CommandMessage;

    /// <summary>
    ///     Start processing Home Assistant events
    /// </summary>
    /// <param name="cancelToken">The token to cancel the processing of events</param>
    Task ProcessHomeAssistantEventsAsync(CancellationToken cancelToken);
}

/// <summary>
///     Access to all raw Home Assistant messages
/// </summary>
public interface IHomeAssistantHassMessages
{
    /// <summary>
    ///     Allows subsription on all Home Assistant Messages
    /// </summary>
    /// <remark>
    ///     This requires that "ProcessHomeAssistantEvents" task is running
    /// </remark>
    IObservable<HassMessage> HassMessages { get; }
}