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

app.MapPost("/add", (int a, int b, IHttpClientFactory clientFactory) =>
{
    var result = a + b;

    var historyClient = clientFactory.CreateClient("History");
    historyClient.PostAsJsonAsync("remember", new HistoryEntry("add", result));

    return result;
});

app.MapPost("/subtract", (int a, int b, IHttpClientFactory clientFactory) =>
{
    var result = a - b;

    var historyClient = clientFactory.CreateClient("History");
    historyClient.PostAsJsonAsync("remember", new HistoryEntry("subtract", result));

    return result;
});

app.Run();