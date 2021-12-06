using Netdaemon.Client.Common;
using NetDaemon.Client.Internal.Net;
using NetDaemon.Client.Internal.HomeAssistant.Commands;
using NetDaemon.Client.Internal.HomeAssistant.Messages;

namespace NetDaemon.Client.Internal;

internal class HomeAssistantRunner : IHomeAssistantRunner
{
    public IObservable<IHomeAssistantConnection> OnConnect => throw new NotImplementedException();

    public IObservable<DisconnectReason> OnDisconnect => throw new NotImplementedException();

    public Task RunAsync(string host, int port, bool ssl, string token, CancellationToken cancelToken)
    {
        throw new NotImplementedException();
    }
}