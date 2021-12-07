using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NetDaemon.HassClient.Tests.Integration;
/// <summary>
///     The Home Assistant Mock class implements a fake Home Assistant server by
///     exposing the websocket api and fakes responses to requests.
/// </summary>
public class HomeAssistantMock : IAsyncDisposable
{
    public static readonly int RecieiveBufferSize = 1024 * 4;
    public readonly IHost HomeAssistantHost;

    public int ServerPort { get; }

    public HomeAssistantMock()
    {
        HomeAssistantHost = CreateHostBuilder().Build() ?? throw new ApplicationException("Failed to create host");
        HomeAssistantHost.Start();
        var server = HomeAssistantHost.Services.GetRequiredService<IServer>();
        var addressFeature = server.Features.Get<IServerAddressesFeature>();
        foreach (var address in addressFeature?.Addresses)
        {
            ServerPort = int.Parse(address.Split(':').Last());
            break;
        }
    }

    /// <summary>
    ///     Starts a websocket server in a generic host
    /// </summary>
    /// <returns>Returns a IHostBuilder instance</returns>
    public static IHostBuilder CreateHostBuilder() =>
        Host.CreateDefaultBuilder()
            .ConfigureServices(s => s.AddHttpClient())
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseUrls("http://127.0.0.1:0"); //"http://172.17.0.2:5001"
                webBuilder.UseStartup<HassMockStartup>();
            });


    /// <summary>
    ///     Stops the fake Home Assistant server
    /// </summary>
    public async Task Stop()
    {
        await HomeAssistantHost.StopAsync().ConfigureAwait(false);
        await HomeAssistantHost.WaitForShutdownAsync().ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await Stop().ConfigureAwait(false);
    }
}

/// <summary>
///     The class implementing the mock hass server
/// </summary>
public class HassMockStartup
{
    private readonly CancellationTokenSource cancelSource = new CancellationTokenSource();

    // Home Assistant will always prettyprint responses so so do the mock
    private readonly byte[] _authOkMessage =
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Integration", "Testdata", "auth_ok.json"));

    // Get the path to mock testdata
    private readonly string _mockTestdataPath = Path.Combine(AppContext.BaseDirectory, "Integration", "Testdata");

    private readonly string _pongMessage =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Integration", "Testdata", "pong.json"));

    private readonly string _resultMessage =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Integration", "Testdata", "result_msg.json"));

    private readonly string _resultConfigMessage =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Integration", "Testdata", "result_config.json"));

    private readonly string _eventMessage =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Integration", "Testdata", "event.json"));

    private readonly JsonSerializerOptions serializeOptions = new JsonSerializerOptions { WriteIndented = true };

    public HassMockStartup(IConfiguration configuration) => Configuration = configuration;

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection _)
    {
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment _)
    {
        var webSocketOptions = new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromSeconds(120)
            // ReceiveBufferSize = HomeAssistantMock.RecieiveBufferSize
        };
        app.UseWebSockets(webSocketOptions);
        app.Map("/api/websocket", builder =>
        {
            builder.Use(async (context, next) =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

                    await ProcessWS(webSocket);
                    return;
                }

                await next();
            });
        });
    }

    private int GetIdFromCommand(byte[] buffer, int len)
    {
        var subscribeEventMessage =
            JsonSerializer.Deserialize<SendCommandMessage>(
            new ReadOnlySpan<byte>(buffer, 0, len));
        return subscribeEventMessage?.Id ?? 0;
    }

    private static async Task ReplaceIdInResponseAndSendMsg(string responseMessageFileName, int id, WebSocket websocket)
    {
        var msg = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Integration", "Testdata", responseMessageFileName));
        // All testdata has id=3 so easy to replace it
        msg = msg.Replace("\"id\": 3", $"\"id\": {id}");
        var bytes = Encoding.UTF8.GetBytes(msg);

        await websocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length),
            WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
    }
    /// <summary>
    ///     Process incoming websocket requests to simulate Home Assistant websocket API
    /// </summary>
    private async Task ProcessWS(WebSocket webSocket)
    {
        // Buffer is set.
        byte[] buffer = new byte[HomeAssistantMock.RecieiveBufferSize];

        try
        {
            // First send auth required to the client
            byte[] authRequiredMessage = File.ReadAllBytes(Path.Combine(_mockTestdataPath, "auth_required.json"));
            await webSocket.SendAsync(new ArraySegment<byte>(authRequiredMessage, 0, authRequiredMessage.Length),
                WebSocketMessageType.Text, true, cancelSource.Token).ConfigureAwait(false);

            // Wait for incoming messages
            WebSocketReceiveResult result =
                await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancelSource.Token).ConfigureAwait(false);

            // Console.WriteLine($"SERVER: WebSocketState = {webSocket.State}, MessageType = {result.MessageType}");
            while (!result.CloseStatus.HasValue)
            {
                var hassMessage =
                    JsonSerializer.Deserialize<HassMessage>(new ReadOnlySpan<byte>(buffer, 0, result.Count))
                    ?? throw new ApplicationException("Unexpected not able to deserialzie");
                switch (hassMessage.Type)
                {
                    // We have an auth message
                    case "auth":
                        var authMessage =
                            JsonSerializer.Deserialize<AuthMessage>(
                                new ReadOnlySpan<byte>(buffer, 0, result.Count));
                        if (authMessage?.AccessToken == "ABCDEFGHIJKLMNOPQ")
                        {
                            // Hardcoded to be correct for test-case
                            // byte[] authOkMessage = File.ReadAllBytes (Path.Combine (this.mockTestdataPath, "auth_ok.json"));
                            await webSocket.SendAsync(
                                new ArraySegment<byte>(_authOkMessage, 0, _authOkMessage.Length),
                                WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
                        }
                        else
                        {
                            // Hardcoded to be correct for test-case
                            byte[] authNotOkMessage =
                                File.ReadAllBytes(Path.Combine(_mockTestdataPath, "auth_notok.json"));
                            await webSocket.SendAsync(
                                new ArraySegment<byte>(authNotOkMessage, 0, authNotOkMessage.Length),
                                WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
                            // Hass will normally close session here but for the sake of testing it wont
                        }

                        break;
                    case "ping":
                        await ReplaceIdInResponseAndSendMsg(
                            "pong.json",
                            hassMessage.Id,
                            webSocket).ConfigureAwait(false);
                        break;
                    case "subscribe_events":
                        await ReplaceIdInResponseAndSendMsg(
                            "result_msg.json",
                            hassMessage.Id,
                            webSocket).ConfigureAwait(false);

                        await ReplaceIdInResponseAndSendMsg(
                             "event.json",
                             hassMessage.Id,
                             webSocket).ConfigureAwait(false);
                        break;
                    case "get_states":
                        await ReplaceIdInResponseAndSendMsg(
                            "result_states.json",
                            hassMessage.Id,
                            webSocket).ConfigureAwait(false);
                        break;
                    case "get_services":
                        await ReplaceIdInResponseAndSendMsg(
                            "result_get_services.json",
                            hassMessage.Id,
                            webSocket).ConfigureAwait(false);
                        break;
                    case "get_config":
                        await ReplaceIdInResponseAndSendMsg(
                            "result_config.json",
                            hassMessage.Id,
                            webSocket).ConfigureAwait(false);
                        break;
                    case "config/area_registry/list":
                        await ReplaceIdInResponseAndSendMsg(
                            "result_get_areas.json",
                            hassMessage.Id,
                            webSocket).ConfigureAwait(false);

                        break;
                    case "config/device_registry/list":
                        await ReplaceIdInResponseAndSendMsg(
                            "result_get_devices.json",
                            hassMessage.Id,
                            webSocket).ConfigureAwait(false);
                        break;
                    case "config/entity_registry/list":
                        await ReplaceIdInResponseAndSendMsg(
                                "result_get_entities.json",
                                hassMessage.Id,
                                webSocket).ConfigureAwait(false);
                        break;
                    case "fake_disconnect_test":
                        // This is not a real home assistant message, just used to test disconnect from socket.
                        // This one tests a normal disconnect
                        var timeout = new CancellationTokenSource(5000);
                        try
                        {
                            // Send close message (some bug n CloseAsync makes we have to do it this way)
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing",
                                timeout.Token).ConfigureAwait(false);
                            // Wait for close message
                            //await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), timeout.Token);
                        }
                        catch (OperationCanceledException)
                        {
                        }

                        return;
                }

                // Wait for incoming messages
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).ConfigureAwait(false);
            }

            await webSocket.CloseOutputAsync(result.CloseStatus.Value, result.CloseStatusDescription,
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal", CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            throw new ApplicationException("The thing is closed unexpectedly", e);
        }
    }
    private class AuthMessage
    {
        [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = string.Empty;
    }
    private class SendCommandMessage
    {
        [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
        [JsonPropertyName("id")] public int Id { get; set; } = 0;
    }
}