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
        var addressFeature = server.Features.Get<IServerAddressesFeature>() ?? throw new NullReferenceException();
        foreach (var address in addressFeature.Addresses)
        {
            if (address is not null)
            {
                ServerPort = int.Parse(address.Split(':').Last());
                break;
            }
        }
    }

    /// <summary>
    ///     Starts a websocket server in a generic host
    /// </summary>
    /// <returns>Returns a IHostBuilder instance</returns>
    public static IHostBuilder CreateHostBuilder() =>
        Host.CreateDefaultBuilder()
            .ConfigureServices(s =>
            {
                s.AddHttpClient();
                s.Configure<HostOptions>(
                    opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(30)
                );
            })
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
        GC.SuppressFinalize(this);
    }
}

/// <summary>
///     The class implementing the mock hass server
/// </summary>
public class HassMockStartup : IHostedService
{
    private readonly CancellationTokenSource cancelSource = new();

    private static int DefaultTimeOut => 5000;

    private readonly byte[] _authOkMessage =
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Integration", "Testdata", "auth_ok.json"));

    // Get the path to mock testdata
    private readonly string _mockTestdataPath = Path.Combine(AppContext.BaseDirectory, "Integration", "Testdata");

    public HassMockStartup(IConfiguration configuration) => Configuration = configuration;

    public IConfiguration Configuration { get; }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment e)
    {
        var webSocketOptions = new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromSeconds(120)
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
        app.UseRouting();
        // app.Map("/api/devices", builder =>
        // {
        //     builder.Use(async (context, next) =>
        //     {


        //         await next();
        //     });
        // });

        app.UseEndpoints(
            e =>
            {
                e.Map("/api/devices",
                     async c =>

                     {
                         await ProcessRequest(c).ConfigureAwait(false);
                     }
                );
            });




        // app.Map("/api", builder =>
        // {
        //     builder.Use(async (context, next) =>
        //     {
        //         await ProcessRequest(context).ConfigureAwait(false);
        //         await next().ConfigureAwait(false);
        //     });
        // });
    }


    // For testing the API we just return a entity
    private async Task ProcessRequest(HttpContext context)
    {
        var entityName = "test.entity";
        if (context.Request.Method == "POST")
            entityName = "test.post";

        await context.Response.WriteAsJsonAsync<HassEntity>(
            new HassEntity
            {
                EntityId = entityName,
                DeviceId = "ksakksk22kssk2",
                AreaId = "ssksks2ksk3k333kk",
                Name = "name"
            }
        ).ConfigureAwait(false);

    }

    /// <summary>
    ///     Replaces the id of the result being sent by the id of the command reveived
    /// </summary>
    /// <param name="responseMessageFileName">Filename of the result</param>
    /// <param name="id">Id of the command</param>
    /// <param name="websocket">The websocket to send to</param>
    private async Task ReplaceIdInResponseAndSendMsg(string responseMessageFileName, int id, WebSocket websocket)
    {
        var msg = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Integration", "Testdata", responseMessageFileName));
        // All testdata has id=3 so easy to replace it
        msg = msg.Replace("\"id\": 3", $"\"id\": {id}");
        var bytes = Encoding.UTF8.GetBytes(msg);

        await websocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length),
            WebSocketMessageType.Text, true, cancelSource.Token).ConfigureAwait(false);
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


            // Console.WriteLine($"SERVER: WebSocketState = {webSocket.State}, MessageType = {result.MessageType}");
            while (true)
            {
                // Wait for incoming messages
                WebSocketReceiveResult result =
                    await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancelSource.Token).ConfigureAwait(false);

                cancelSource.Token.ThrowIfCancellationRequested();

                if (result.CloseStatus.HasValue && webSocket.State == WebSocketState.CloseReceived)
                    break;

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
                                WebSocketMessageType.Text, true, cancelSource.Token).ConfigureAwait(false);
                        }
                        else
                        {
                            // Hardcoded to be correct for test-case
                            byte[] authNotOkMessage =
                                File.ReadAllBytes(Path.Combine(_mockTestdataPath, "auth_notok.json"));
                            await webSocket.SendAsync(
                                new ArraySegment<byte>(authNotOkMessage, 0, authNotOkMessage.Length),
                                WebSocketMessageType.Text, true, cancelSource.Token).ConfigureAwait(false);
                            // Hass will normally close session here but for the sake of testing the mock wont
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
                    case "call_service":
                        await ReplaceIdInResponseAndSendMsg(
                            "result_msg.json",
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
                    case "fake_return_error":
                        await ReplaceIdInResponseAndSendMsg(
                                "result_msg_error.json",
                                hassMessage.Id,
                                webSocket).ConfigureAwait(false);
                        break;
                    case "fake_service_event":
                        // Here we fake the server sending a service
                        // event by returning success and then 
                        // return a service event
                        await ReplaceIdInResponseAndSendMsg(
                                "result_msg.json",
                                hassMessage.Id,
                                webSocket).ConfigureAwait(false);

                        await ReplaceIdInResponseAndSendMsg(
                                "service_event.json",
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
            }
        }
        catch (OperationCanceledException)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal", CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            throw new ApplicationException("The thing is closed unexpectedly", e);
        }
        finally
        {
            try
            {
                await SendCorrectCloseFrameToRemoteWebSocket(webSocket).ConfigureAwait(false);
            }
            finally
            {
                // Just fail silently                
            }
        }
    }

    /// <summary>
    ///     Closes correctly the websocket depending on websocket state
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Closing a websocket has special handling. When the client
    ///     wants to close it calls CloseAsync and the websocket takes
    ///     care of the proper close handling.
    /// </para>
    /// <para>
    ///     If the remote websocket wants to close the connection dotnet
    ///     implementation requires you to use CloseOutputAsync instead.
    /// </para>
    /// <para>
    ///     We do not want to cancel operations until we get closed state
    ///     this is why own timer cancellation token is used and we wait
    ///     for correct state before returning and disposing any connections
    /// </para>
    /// </remarks>
    private async Task SendCorrectCloseFrameToRemoteWebSocket(WebSocket _ws)
    {
        using var timeout = new CancellationTokenSource(DefaultTimeOut);

        try
        {
            if (_ws.State == WebSocketState.CloseReceived)
            {
                // after this, the socket state which change to CloseSent
                await _ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing", timeout.Token).ConfigureAwait(false);
                // now we wait for the server response, which will close the socket
                while (_ws.State != WebSocketState.Closed && !timeout.Token.IsCancellationRequested)
                    await Task.Delay(100).ConfigureAwait(false);
            }
            else if (_ws.State == WebSocketState.Open)
            {
                // Do full close 
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", timeout.Token).ConfigureAwait(false);
                if (_ws.State != WebSocketState.Closed)
                    throw new ApplicationException("Expected the websocket to be closed!");
            }
        }
        catch (OperationCanceledException)
        {
            // normal upon task/token cancellation, disregard
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        cancelSource.Cancel();
        return Task.CompletedTask;
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