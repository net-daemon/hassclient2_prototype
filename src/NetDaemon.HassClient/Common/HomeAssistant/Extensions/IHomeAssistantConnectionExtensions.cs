using Netdaemon.Client.Common;
using NetDaemon.Client.Common.HomeAssistant.Model;
using NetDaemon.Client.Internal.HomeAssistant.Commands;

namespace NetDaemon.Client.Common.HomeAssistant.Extensions;
public static class IHomeAssistantConnectionExtensions
{
    /// <summary>
    ///     Get all states from all entities from Home Assistant
    /// </summary>
    /// <param name="connection">connected Home Assistant instance</param>
    /// <param name="cancelToken">cancellation token</param>
    public static async Task<IReadOnlyCollection<HassState>?> GetStatesAsync(this IHomeAssistantConnection connection, CancellationToken cancelToken)
    => await connection
            .SendCommandAndReturnResponseAsync<SimpleCommand, IReadOnlyCollection<HassState>>
                   (new("get_states"), cancelToken).ConfigureAwait(false);

    /// <summary>
    ///     Get all services from Home Assistant
    /// </summary>
    /// <param name="connection">connected Home Assistant instance</param>
    /// <param name="cancelToken">cancellation token</param>
    public static async Task<IReadOnlyCollection<HassServiceDomain>?> GetServicesAsync(this IHomeAssistantConnection connection, CancellationToken cancelToken)
    => await connection
            .SendCommandAndReturnResponseAsync<SimpleCommand, IReadOnlyCollection<HassServiceDomain>>
                   (new("get_services"), cancelToken).ConfigureAwait(false);

    /// <summary>
    ///     Get all areas from Home Assistant
    /// </summary>
    /// <param name="connection">connected Home Assistant instance</param>
    /// <param name="cancelToken">cancellation token</param>
    public static async Task<IReadOnlyCollection<HassArea>?> GetAreasAsync(this IHomeAssistantConnection connection, CancellationToken cancelToken)
    => await connection
            .SendCommandAndReturnResponseAsync<SimpleCommand, IReadOnlyCollection<HassArea>>
                   (new("config/area_registry/list"), cancelToken).ConfigureAwait(false);

    /// <summary>
    ///     Get all devices from Home Assistant
    /// </summary>
    /// <param name="connection">connected Home Assistant instance</param>
    /// <param name="cancelToken">cancellation token</param>
    public static async Task<IReadOnlyCollection<HassDevice>?> GetDevicesAsync(this IHomeAssistantConnection connection, CancellationToken cancelToken)
    => await connection
            .SendCommandAndReturnResponseAsync<SimpleCommand, IReadOnlyCollection<HassDevice>>
                   (new("config/device_registry/list"), cancelToken).ConfigureAwait(false);

    /// <summary>
    ///     Get all entites from Home Assistant
    /// </summary>
    /// <param name="connection">connected Home Assistant instance</param>
    /// <param name="cancelToken">cancellation token</param>
    public static async Task<IReadOnlyCollection<HassEntity>?> GetEntitiesAsync(this IHomeAssistantConnection connection, CancellationToken cancelToken)
    => await connection
            .SendCommandAndReturnResponseAsync<SimpleCommand, IReadOnlyCollection<HassEntity>>
                   (new("config/entity_registry/list"), cancelToken).ConfigureAwait(false);

    /// <summary>
    ///     Get all configuration from Home Assistant
    /// </summary>
    /// <param name="connection">connected Home Assistant instance</param>
    /// <param name="cancelToken">cancellation token</param>
    public static async Task<HassConfig> GetConfigAsync(this IHomeAssistantConnection connection, CancellationToken cancelToken)
    => await connection
        .SendCommandAndReturnResponseAsync<SimpleCommand, HassConfig>
                    (new("get_config"), cancelToken).ConfigureAwait(false) ??
                        throw new NullReferenceException("Unexpected null return from command");

    /// <summary>
    ///     Pings the connected Home Assistant instance and expect a pong
    /// </summary>
    /// <param name="connection">connected Home Assistant instance</param>
    /// <param name="timeout">Timeout to wait for pong back</param>
    /// <param name="cancelToken">cancellation token</param>
    public static async Task<bool> PingAsync(this IHomeAssistantConnection connection, TimeSpan timeout, CancellationToken cancelToken)
    {
        var allHassMessages = connection as IHomeAssistantHassMessages
            ?? throw new InvalidCastException("Unexpected failure to cast");
        try
        {
            var resultEvent = allHassMessages.OnHassMessage
                .Where(n => n.Type == "pong")
                .Timeout(timeout, Observable.Return(default(HassMessage?)))
                .FirstAsync()
                .ToTask();

            await connection
                .SendCommandAsync<SimpleCommand>
                        (new("ping"), cancelToken).ConfigureAwait(false);

            await resultEvent.ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            return false;
        }
        return true;
    }
}
