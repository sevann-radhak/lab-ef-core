using LabEfCore.Api;
using LabEfCore.Api.Data;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<QueryCounter>();
builder.Services.AddDbContext<SalesDbContext>((services, options) =>
{
    string connectionString = builder.Configuration.GetConnectionString("Sales")
        ?? "Data Source=sales.db";
    _ = options.UseSqlite(connectionString);
    _ = options.AddInterceptors(services.GetRequiredService<QueryCounter>());
});

builder.Services.AddOpenApi(options =>
{
    _ = options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "lab-ef-core";
        document.Info.Version = "v1";
        document.Info.Description =
            """
            Order list and note update over EF Core and SQLite.

            Compare queryCount on the list operations. The N+1 route runs one query for the orders and then more queries per row. Include and the projection each return the same rows with fewer statements.

            GET /orders/composed keeps Where on IQueryable, so the filter is SQL. GET /orders/in-memory executes first and filters the List in the process. Pass customer, for example Ada Bakery.

            A tracked note update is persisted. A note update loaded with AsNoTracking is not.
            """;
        return Task.CompletedTask;
    });
});

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    _ = app.MapOpenApi();
    _ = app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "lab-ef-core v1");
        options.DocumentTitle = "lab-ef-core";
        options.DisplayRequestDuration();
        options.EnableTryItOutByDefault();
    });
}

await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    await SalesSeed.ApplyAsync(scope.ServiceProvider.GetRequiredService<SalesDbContext>());
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("GetHealth")
    .WithTags("Health")
    .WithSummary("Liveness probe")
    .WithDescription("Returns 200 when the process can serve HTTP. It does not check the database.")
    .AllowAnonymous();

OrderEndpoints.Map(app);

app.Run();

public partial class Program;
