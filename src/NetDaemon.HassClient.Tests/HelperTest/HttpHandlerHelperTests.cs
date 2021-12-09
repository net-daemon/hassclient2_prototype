using NetDaemon.Client.Internal.Helpers;
namespace NetDaemon.HassClient.Tests.HelperTest;

public class HttpHandlerHelperTests
{
    [Fact]
    public void TestHttpHandlerHelperCreateClient()
    {
        var client = HttpHelper.CreateHttpClient();
        client.Should().BeOfType<HttpClient>();
    }

    [Fact]
    public void TestHttpHandlerHelperCreateHttpMessageHandler()
    {
        var client = HttpHelper.CreateHttpMessageHandler();
        client.Should().BeOfType<HttpClientHandler>();
    }
}