using Netdaemon.Client.Common;
using NetDaemon.Client.Common.Net;
using NetDaemon.Client.Internal.HomeAssistant.Commands;
using NetDaemon.Client.Internal.HomeAssistant.Messages;

namespace NetDaemon.Client.Internal;

public class HomeAssistantClient : IHomeAssistantClient
{
    private readonly ILogger<HomeAssistantClient> _logger;
    private readonly IWebSocketClientFactory _webSocketClientFactory;
    private readonly IWebSocketClientTransportPipelineFactory _transportPipelineFactory;
    private readonly IHomeAssistantConnectionFactory _connectionFactory;

    public HomeAssistantClient(
        ILogger<HomeAssistantClient> logger,
        IWebSocketClientFactory WebSocketClientFactory,
        IWebSocketClientTransportPipelineFactory transportPipelineFactory,
        IHomeAssistantConnectionFactory connectionFactory
    )
    {
        _logger = logger;
        _webSocketClientFactory = WebSocketClientFactory;
        _transportPipelineFactory = transportPipelineFactory;
        _connectionFactory = connectionFactory;
    }

    private static Uri GetHomeAssistantWebSocketUri(string host, int port, bool ssl)
    {
        return new($"{(ssl ? "wss" : "ws")}://{host}:{port}/api/websocket");
    }

    public async Task<IHomeAssistantConnection> ConnectAsync(string host, int port, bool ssl, string token, CancellationToken cancelToken)
    {
        var websocketUri = GetHomeAssistantWebSocketUri(host, port, ssl);

        var ws = _webSocketClientFactory.New();

        try
        {
            await ws.ConnectAsync(websocketUri, cancelToken).ConfigureAwait(false);

            var transportPipeline = _transportPipelineFactory.New(ws);

            await HandleAutorizationSequence(token, transportPipeline, cancelToken).ConfigureAwait(false);

            return _connectionFactory.New(transportPipeline);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Connect to Home Assistant was cancelled");
            throw;
        }
        catch (Exception e)
        {
            _logger.LogDebug("Error connecting to Home Assistant", e);
            throw;
        }
    }

    private static async Task HandleAutorizationSequence(string token, IWebSocketClientTransportPipeline transportPipeline, CancellationToken cancelToken)
    {
        var connectTimeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
        connectTimeoutTokenSource.CancelAfter(5000);
        // Begin the autorization sequence
        // Expect 'auth_required' 
        var msg = await transportPipeline.GetNextMessageAsync<HassMessage>(connectTimeoutTokenSource.Token).ConfigureAwait(false);
        if (msg.Type != "auth_required")
            throw new ApplicationException($"Unexpected type: '{msg.Type}' expected 'auth_required'");

        // Now send the auth message to Home Assistant
        await transportPipeline.SendMessageAsync(
            new HassAuthMessage { AccessToken = token },
            connectTimeoutTokenSource.Token
        ).ConfigureAwait(false);
        // Now get the result
        var authResultMessage = await transportPipeline.GetNextMessageAsync<HassMessage>(connectTimeoutTokenSource.Token).ConfigureAwait(false);

        switch (authResultMessage.Type)
        {
            case "auth_ok":
                return;

            case "auth_invalid":
                throw new ApplicationException("Failed to authenticate token");

            default:
                throw new ApplicationException($"Unexpected response ({authResultMessage.Type})");
        }
    }
}
