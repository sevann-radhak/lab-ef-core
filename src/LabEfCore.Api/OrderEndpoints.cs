using LabEfCore.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace LabEfCore.Api;

public static class OrderEndpoints
{
    public static void Map(WebApplication app)
    {
        _ = app.MapGet("/orders/n-plus-one", GetNPlusOne)
            .WithName("GetOrdersNPlusOne")
            .WithTags("Queries")
            .WithSummary("Load orders, then query each row again")
            .WithDescription(
                "ToListAsync turns the IQueryable into a List. The loop then runs a new query per order. " +
                "queryCount grows with the number of orders. The response shape matches the other list operations.")
            .Produces<QueryResult>(StatusCodes.Status200OK);

        _ = app.MapGet("/orders/include", GetIncluded)
            .WithName("GetOrdersInclude")
            .WithTags("Queries")
            .WithSummary("Load orders with customer and lines in the first query")
            .WithDescription(
                "Include stays on the IQueryable, so customer and lines are part of the SQL. " +
                "After ToListAsync the Select runs as IEnumerable in memory, over entities already loaded.")
            .Produces<QueryResult>(StatusCodes.Status200OK);

        _ = app.MapGet("/orders/projected", GetProjected)
            .WithName("GetOrdersProjected")
            .WithTags("Queries")
            .WithSummary("Select only the columns the response needs")
            .WithDescription(
                "Project returns IQueryable of OrderRow. ToListAsync is the execution point. " +
                "The SQL selects order id, number, customer name, and line count.")
            .Produces<QueryResult>(StatusCodes.Status200OK);

        _ = app.MapGet("/orders/composed", GetComposed)
            .WithName("GetOrdersComposed")
            .WithTags("LINQ")
            .WithSummary("Filter on IQueryable, then execute once")
            .WithDescription(
                "Query parameter customer is required. Where is applied to IQueryable before ToListAsync, so SQL receives the predicate. " +
                "loadedCount matches the number of items. Example: Ada Bakery.")
            .Produces<FilteredQueryResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        _ = app.MapGet("/orders/in-memory", GetFilteredInMemory)
            .WithName("GetOrdersFilteredInMemory")
            .WithTags("LINQ")
            .WithSummary("Load every row, then filter the List")
            .WithDescription(
                "Query parameter customer is required. The query executes first. Where then runs on IEnumerable in the process. " +
                "queryCount stays 1, and loadedCount is the full table. Example: Ada Bakery.")
            .Produces<FilteredQueryResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        _ = app.MapPost("/orders/{id:int}/note", UpdateTracked)
            .WithName("UpdateOrderNoteTracked")
            .WithTags("Change tracking")
            .WithSummary("Update a note on a tracked order")
            .WithDescription(
                "The order is loaded without AsNoTracking, so it is tracked. Assigning Note marks it Modified. " +
                "SaveChanges writes one UPDATE. Path parameter id is the order id from a list response.")
            .Produces<NoteResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        _ = app.MapPost("/orders/{id:int}/note/detached", UpdateDetached)
            .WithName("UpdateOrderNoteDetached")
            .WithTags("Change tracking")
            .WithSummary("Change a detached order and save nothing")
            .WithDescription(
                "AsNoTracking loads the order without a tracker entry. Changing Note in memory does not mark a database update. " +
                "SaveChanges returns 0. databaseNote is read again and still has the stored value.")
            .Produces<NoteResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetNPlusOne(SalesDbContext db, QueryCounter counter)
    {
        List<Order> orders = await db.Orders.AsNoTracking().OrderBy(order => order.Id).ToListAsync();
        List<OrderRow> rows = [];

        foreach (Order order in orders)
        {
            string customerName = await db.Customers.AsNoTracking()
                .Where(customer => customer.Id == order.CustomerId)
                .Select(customer => customer.Name)
                .FirstAsync();
            int lineCount = await db.OrderLines.AsNoTracking().CountAsync(line => line.OrderId == order.Id);
            rows.Add(new OrderRow(order.Id, order.Number, customerName, lineCount));
        }

        return Results.Ok(new QueryResult(counter.Count, rows));
    }

    private static async Task<IResult> GetIncluded(SalesDbContext db, QueryCounter counter)
    {
        List<Order> orders = await db.Orders.AsNoTracking()
            .Include(order => order.Customer)
            .Include(order => order.Lines)
            .OrderBy(order => order.Id)
            .ToListAsync();

        IEnumerable<OrderRow> rows = orders.Select(order =>
            new OrderRow(order.Id, order.Number, order.Customer.Name, order.Lines.Count));

        return Results.Ok(new QueryResult(counter.Count, rows.ToList()));
    }

    private static async Task<IResult> GetProjected(SalesDbContext db, QueryCounter counter)
    {
        IQueryable<OrderRow> query = OrderQueries.Project(db.Orders.AsNoTracking());
        List<OrderRow> rows = await query.ToListAsync();
        return Results.Ok(new QueryResult(counter.Count, rows));
    }

    private static async Task<IResult> GetComposed(string? customer, SalesDbContext db, QueryCounter counter)
    {
        if (string.IsNullOrWhiteSpace(customer))
        {
            return MissingCustomer();
        }

        IQueryable<Order> filtered = OrderQueries.ForCustomer(db.Orders.AsNoTracking(), customer);
        List<OrderRow> rows = await OrderQueries.Project(filtered).ToListAsync();

        return Results.Ok(new FilteredQueryResult(counter.Count, rows.Count, "database", rows));
    }

    private static async Task<IResult> GetFilteredInMemory(string? customer, SalesDbContext db, QueryCounter counter)
    {
        if (string.IsNullOrWhiteSpace(customer))
        {
            return MissingCustomer();
        }

        List<OrderRow> loaded = await OrderQueries.Project(db.Orders.AsNoTracking()).ToListAsync();
        List<OrderRow> rows = OrderQueries.ForCustomerInMemory(loaded, customer).ToList();

        return Results.Ok(new FilteredQueryResult(counter.Count, loaded.Count, "memory", rows));
    }

    private static async Task<IResult> UpdateTracked(int id, NoteRequest request, SalesDbContext db, QueryCounter counter)
    {
        Order? order = await db.Orders.FirstOrDefaultAsync(item => item.Id == id);

        if (order is null)
        {
            return Results.NotFound();
        }

        order.Note = request.Note;
        int saved = await db.SaveChangesAsync();

        return Results.Ok(new NoteResult(saved, order.Note, counter.Count));
    }

    private static async Task<IResult> UpdateDetached(int id, NoteRequest request, SalesDbContext db, QueryCounter counter)
    {
        Order? order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

        if (order is null)
        {
            return Results.NotFound();
        }

        order.Note = request.Note;
        int saved = await db.SaveChangesAsync();
        string? databaseNote = await db.Orders.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => item.Note)
            .FirstAsync();

        return Results.Ok(new NoteResult(saved, databaseNote, counter.Count));
    }

    private static IResult MissingCustomer()
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["customer"] = ["customer is required."],
        });
    }
}
