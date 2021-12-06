using Netdaemon.Client.Common;

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