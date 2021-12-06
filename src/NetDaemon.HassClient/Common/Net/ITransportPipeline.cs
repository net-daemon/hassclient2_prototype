using System.Net.WebSockets;

namespace NetDaemon.Client.Common.Net;
/// <summary>
///     The pipline makes a transport layer on top of WebSocketClient.
///     This pipeline handles json serialization
/// </summary>
public interface IWebSocketClientTransportPipeline : IAsyncDisposable
{
    /// <summary>
    ///     Gets next message from pipeline
    /// </summary>
    ValueTask<T> GetNextMessageAsync<T>(CancellationToken cancellationToken) where T : class;

    /// <summary>
    ///     Sends a message to the pipline
    /// </summary>
    /// <param name="message"></param>
    Task SendMessageAsync<T>(T message, CancellationToken cancellationToken) where T : class;

    /// <summary>
    ///     Close the pipeline, it will also close the underlying websocket
    /// </summary>
    Task CloseAsync();

    /// <summary>
    ///     State of the underlying websocket
    /// </summary>
    WebSocketState WebSocketState { get; }
}