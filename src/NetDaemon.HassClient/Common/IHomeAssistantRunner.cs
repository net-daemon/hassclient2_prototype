namespace Netdaemon.Client.Common;

public interface IHomeAssistantRunner
{
    /// <summary>
    ///     Maintains a connection to the Home Assistant server
    /// </summary>
    /// <param name="host">Host of Home Assistant instance</param>
    /// <param name="port">Port of Home Assistant instance</param>
    /// <param name="ssl">Use ssl</param>
    /// <param name="token">Home Assistant secret token</param>
    /// <param name="cancelToken">Cancel token</param>
    Task RunAsync(string host, int port, bool ssl, string token, CancellationToken cancelToken);

    /// <summary>
    ///     Event when new connection is established
    /// </summary>
    IObservable<IHomeAssistantConnection> OnConnect { get; }

    /// <summary>
    ///     Event when connection is lost
    /// </summary>
    IObservable<DisconnectReason> OnDisconnect { get; }
}