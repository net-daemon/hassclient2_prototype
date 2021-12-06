namespace NetDaemon.Client.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHomeAssistantClient(this IServiceCollection services)
    {
        services.AddSingleton<HomeAssistantClient>();
        services.AddSingleton<IHomeAssistantClient>(s => s.GetRequiredService<HomeAssistantClient>());
        services.AddSingleton<HomeAssistantRunner>();
        services.AddSingleton<IHomeAssistantRunner>(s => s.GetRequiredService<HomeAssistantRunner>());
        services.AddWebSocketFactory();
        services.AddPipelineFactory();
        services.AddConnectionFactory();
        return services;
    }

    internal static IServiceCollection AddWebSocketFactory(this IServiceCollection services)
    {
        services.AddSingleton<WebSocketClientFactory>();
        services.AddSingleton<IWebSocketClientFactory>(s => s.GetRequiredService<WebSocketClientFactory>());
        return services;
    }

    internal static IServiceCollection AddPipelineFactory(this IServiceCollection services)
    {
        services.AddSingleton<WebSocketClientTransportPipelineFactory>();
        services.AddSingleton<IWebSocketClientTransportPipelineFactory>(s => s.GetRequiredService<WebSocketClientTransportPipelineFactory>());
        return services;
    }

    internal static IServiceCollection AddConnectionFactory(this IServiceCollection services)
    {
        services.AddSingleton<HomeAssistantConnectionFactory>();
        services.AddSingleton<IHomeAssistantConnectionFactory>(s => s.GetRequiredService<HomeAssistantConnectionFactory>());
        return services;
    }

    internal static IServiceCollection AddHttpClientAndFactory(this IServiceCollection services)
    {
        services.AddSingleton(s => s.GetRequiredService<IHttpClientFactory>().CreateClient());
        services.AddHttpClient<IHomeAssistantApiManager, HomeAssistantApiManager>().ConfigurePrimaryHttpMessageHandler(ConfigureHttpMessageHandler);
        return services;
    }

    internal static HttpMessageHandler ConfigureHttpMessageHandler(IServiceProvider provider)
    {
        var handler = provider.GetService<HttpMessageHandler>();
        return handler ?? HttpHelper.CreateHttpMessageHandler();
    }
}