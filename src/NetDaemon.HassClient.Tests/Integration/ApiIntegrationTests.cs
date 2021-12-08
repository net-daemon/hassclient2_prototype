namespace NetDaemon.HassClient.Tests.Integration;

public class ApiIntegrationTests : IntegrationTestBase
{
    public ApiIntegrationTests(HomeAssistantServiceFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task TestGetApiCall()
    {
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        var entity = await ctx.HomeAssistantConnction.GetApiCall<HassEntity>("devices", _tokenSource.Token).ConfigureAwait(false);

        Assert.NotNull(entity?.EntityId);

        entity!.EntityId
            .Should()
            .BeEquivalentTo("test.entity");
    }

    [Fact]
    public async Task TestPostApiCall()
    {
        await using var ctx = await GetConnectedClientContext().ConfigureAwait(false);
        var entity = await ctx.HomeAssistantConnction.PostApiCall<HassEntity>(
            "devices",
            _tokenSource.Token,
            new { somedata = "hello" }
            ).ConfigureAwait(false);

        Assert.NotNull(entity?.EntityId);

        entity!.EntityId
            .Should()
            .BeEquivalentTo("test.post");
    }
}