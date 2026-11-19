using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace LabEfCore.Tests;

public sealed class OrderQueryTests(SalesApiFactory factory) : IClassFixture<SalesApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Projection_uses_fewer_queries_than_the_per_row_reads()
    {
        QueryPayload projected = await GetOrders("/orders/projected");
        QueryPayload chatty = await GetOrders("/orders/n-plus-one");

        Assert.Equal(1, projected.QueryCount);
        Assert.True(chatty.QueryCount > projected.QueryCount);
        Assert.Equal(projected.Numbers, chatty.Numbers);
        Assert.Contains("Ada Bakery", projected.CustomerNames);
    }

    [Fact]
    public async Task Include_returns_the_same_rows_without_a_query_per_order()
    {
        QueryPayload included = await GetOrders("/orders/include");
        QueryPayload chatty = await GetOrders("/orders/n-plus-one");

        Assert.True(included.QueryCount < chatty.QueryCount);
        Assert.Equal(chatty.Numbers, included.Numbers);
    }

    [Fact]
    public async Task Database_filter_loads_only_matches_and_memory_filter_loads_every_row()
    {
        const string customer = "Ada Bakery";
        FilteredPayload database = await GetFiltered($"/orders/composed?customer={Uri.EscapeDataString(customer)}");
        FilteredPayload memory = await GetFiltered($"/orders/in-memory?customer={Uri.EscapeDataString(customer)}");

        Assert.Equal("database", database.Execution);
        Assert.Equal(1, database.QueryCount);
        Assert.Equal(2, database.LoadedCount);
        Assert.Equal(2, database.Numbers.Length);

        Assert.Equal("memory", memory.Execution);
        Assert.Equal(1, memory.QueryCount);
        Assert.Equal(4, memory.LoadedCount);
        Assert.Equal(database.Numbers, memory.Numbers);
    }

    [Fact]
    public async Task Tracked_note_is_stored()
    {
        string note = $"packed-{Guid.NewGuid():N}";
        NotePayload result = await PostNote("/orders/1/note", note);

        Assert.Equal(1, result.SavedChanges);
        Assert.Equal(note, result.DatabaseNote);
    }

    [Fact]
    public async Task Detached_note_is_not_stored()
    {
        string note = $"ignored-{Guid.NewGuid():N}";
        NotePayload result = await PostNote("/orders/1/note/detached", note);

        Assert.Equal(0, result.SavedChanges);
        Assert.NotEqual(note, result.DatabaseNote);
    }

    [Fact]
    public async Task Swagger_ui_is_available_in_development()
    {
        HttpResponseMessage response = await _client.GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<QueryPayload> GetOrders(string path)
    {
        HttpResponseMessage response = await _client.GetAsync(path);
        _ = response.EnsureSuccessStatusCode();
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = document.RootElement;
        string[] numbers = root.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("number").GetString()!)
            .ToArray();
        string[] names = root.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("customerName").GetString()!)
            .ToArray();

        return new QueryPayload(root.GetProperty("queryCount").GetInt32(), numbers, names);
    }

    private async Task<FilteredPayload> GetFiltered(string path)
    {
        HttpResponseMessage response = await _client.GetAsync(path);
        _ = response.EnsureSuccessStatusCode();
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = document.RootElement;
        string[] numbers = root.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("number").GetString()!)
            .ToArray();

        return new FilteredPayload(
            root.GetProperty("queryCount").GetInt32(),
            root.GetProperty("loadedCount").GetInt32(),
            root.GetProperty("execution").GetString()!,
            numbers);
    }

    private async Task<NotePayload> PostNote(string path, string note)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(path, new { note });
        _ = response.EnsureSuccessStatusCode();
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = document.RootElement;

        return new NotePayload(
            root.GetProperty("savedChanges").GetInt32(),
            root.GetProperty("databaseNote").GetString());
    }

    private sealed record QueryPayload(int QueryCount, string[] Numbers, string[] CustomerNames);

    private sealed record FilteredPayload(int QueryCount, int LoadedCount, string Execution, string[] Numbers);

    private sealed record NotePayload(int SavedChanges, string? DatabaseNote);
}
