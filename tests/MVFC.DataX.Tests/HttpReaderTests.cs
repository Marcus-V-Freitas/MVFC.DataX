namespace MVFC.DataX.Tests;

public sealed class HttpReaderTests
{
    [Fact]
    public async Task HttpApiReader_Deve_retornar_itens_da_api()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var banks = new List<Bank> { new(1, "B1", 1, "B1"), new(2, "B2", 2, "B2") };

        mockHttp.When("https://brasilapi.com.br/api/banks/v1")
                .Respond("application/json", JsonSerializer.Serialize(banks));

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("https://brasilapi.com.br/api");
        var api = RestService.For<IBrasilApi>(client);

        var reader = new HttpApiReader<Bank>(api.GetBanksAsync);

        // Act
        var results = await reader.ReadAsync(TestContext.Current.CancellationToken)
                                  .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        results.Should().HaveCount(2);
        results[0].Name.Should().Be("B1");
    }

    [Fact]
    public async Task HttpApiReader_Deve_lancar_quando_api_falha()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://brasilapi.com.br/api/banks/v1")
                .Respond(HttpStatusCode.InternalServerError);

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("https://brasilapi.com.br/api");
        var api = RestService.For<IBrasilApi>(client);

        var reader = new HttpApiReader<Bank>(api.GetBanksAsync);

        // Act
        Func<Task> act = async () => await reader.ReadAsync(TestContext.Current.CancellationToken)
                                                 .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ApiException>();
    }
}
