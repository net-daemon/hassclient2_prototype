using Netdaemon.Client.Common;
using NetDaemon.Client.Common.Net;
using NetDaemon.Client.Internal;
using NetDaemon.Client.Internal.Net;

namespace NetDaemon.Client.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHomeAssistantClient(this IServiceCollection services)
    {
        services.AddSingleton<HomeAssistantClient>();
        services.AddSingleton<IHomeAssistantClient>(s => s.GetRequiredService<HomeAssistantClient>());
        services.AddWebSocketFactory();
        services.AddPipelineFactory();
        services.AddConnectionFactory();
        return services;
    }

    internal static IServiceCollection AddWebSocketFactory(this IServiceCollection services)
    {
        services.TryAddTransient<IWebSocketClientFactory, WebSocketClientFactory>();
        return services;
    }

    internal static IServiceCollection AddPipelineFactory(this IServiceCollection services)
    {
        services.TryAddTransient<IWebSocketClientTransportPipelineFactory, WebSocketClientTransportPipelineFactory>();
        return services;
    }

    internal static IServiceCollection AddConnectionFactory(this IServiceCollection services)
    {
        services.TryAddTransient<IHomeAssistantConnectionFactory, HomeAssistantConnectionFactory>();
        return services;
    }
}