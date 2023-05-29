var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("History", c =>
{
    c.BaseAddress = new Uri(builder.Configuration.GetValue<string>("HistoryBaseAddress"));
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/add", async (int a, int b, IHttpClientFactory clientFactory) =>
{
    var result = a + b;

    var historyClient = clientFactory.CreateClient("History");
    await historyClient.PostAsJsonAsync("remember", new {Operation = "add", result });

    return result;
});

app.MapGet("/subtract", async (int a, int b, IHttpClientFactory clientFactory) =>
{
    var result = a - b;

    var historyClient = clientFactory.CreateClient("History");
    await historyClient.PostAsJsonAsync("remember", new { Operation = "subtract", result });

    return result;
});

app.Run();