using System.Net.Http.Json;

namespace EndToEndTests;

public class FlowTest : IClassFixture<AppFixture>
{
    private readonly HttpClient _calculatorClient;
    private readonly HttpClient _historyClient;

    public FlowTest(AppFixture appFixture)
    {
        _calculatorClient = appFixture.CalculatorClient;
        _historyClient = appFixture.HistoryClient;
    }

    [Fact]
    public async Task CalculatorOperationsGetSavedToHistory()
    {
        var a = 4;
        var b = 8;
        var additionResult = a + b;
        var subractionResult = a - b;
        
        var historyLog = await _historyClient.GetFromJsonAsync<IEnumerable<HistoryEntry>>("history");
        Assert.NotNull(historyLog);
        Assert.Empty(historyLog);

        var result = await _calculatorClient.GetFromJsonAsync<int>($"add?a={a}&b={b}");
        Assert.Equal(additionResult, result);

        result = await _calculatorClient.GetFromJsonAsync<int>($"subtract?a={a}&b={b}");
        Assert.Equal(subractionResult, result);

        historyLog = await _historyClient.GetFromJsonAsync<IEnumerable<HistoryEntry>>("history");
        Assert.NotNull(historyLog);
        Assert.Equal(new HistoryEntry[]
        {
            new HistoryEntry("add", additionResult),
            new HistoryEntry("subtract", subractionResult)
        }, historyLog);
    }
}