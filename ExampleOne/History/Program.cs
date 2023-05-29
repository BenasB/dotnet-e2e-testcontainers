var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/remember", (HistoryEntry entry) =>
{
    HistoryLog.Entries.Add(entry);
    return Results.Accepted();
});

app.MapGet("/history", () =>
{
    return HistoryLog.Entries;
});

app.Run();

public record HistoryEntry(string Operation, int Result);

static class HistoryLog
{
    public static List<HistoryEntry> Entries = new();
}