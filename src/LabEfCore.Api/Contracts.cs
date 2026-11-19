using System.ComponentModel;

namespace LabEfCore.Api;

public sealed record OrderRow(
    [property: Description("Order id.")]
    int Id,
    [property: Description("Business order number.")]
    string Number,
    [property: Description("Customer name loaded with the order.")]
    string CustomerName,
    [property: Description("Number of lines on the order.")]
    int LineCount);

public sealed record FilteredQueryResult(
    [property: Description("SQL statements executed for this request.")]
    int QueryCount,
    [property: Description("Rows materialized before the response was built. Equals the result size when the filter ran in SQL.")]
    int LoadedCount,
    [property: Description("database when Where ran in SQL. memory when Where ran on a List after the query executed.")]
    string Execution,
    IReadOnlyList<OrderRow> Items);

public sealed record QueryResult(
    [property: Description("SQL statements executed for this request, including the list query and any per-row queries.")]
    int QueryCount,
    IReadOnlyList<OrderRow> Items);

public sealed record NoteRequest(
    [property: Description("Replacement note. Empty is allowed.")]
    string Note);

public sealed record NoteResult(
    [property: Description("Rows written by SaveChanges. Zero means EF had nothing to persist.")]
    int SavedChanges,
    [property: Description("Note currently stored for the order.")]
    string? DatabaseNote,
    [property: Description("SQL statements executed for this request.")]
    int QueryCount);
