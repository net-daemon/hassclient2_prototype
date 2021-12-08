namespace NetDaemon.HassClient.Tests.Integration;

public class ApiIntegrationTests : IntegrationTestBase
{
    public ApiIntegrationTests(HomeAssistantServiceFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task TestSuccessfulConnectShouldReturnConnection()
    {
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        var entity = await ctx.HomeAssistantConnction.GetApiCall<HassEntity>("api/devices", _tokenSource.Token).ConfigureAwait(false);

        Assert.NotNull(entity?.EntityId);

        entity!.EntityId
            .Should()
            .BeEquivalentTo("test.entity");
    }
}