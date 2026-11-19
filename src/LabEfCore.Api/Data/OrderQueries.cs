namespace LabEfCore.Api.Data;

public static class OrderQueries
{
    public static IQueryable<Order> ForCustomer(IQueryable<Order> orders, string customerName)
    {
        return orders.Where(order => order.Customer.Name == customerName);
    }

    public static IQueryable<OrderRow> Project(IQueryable<Order> orders)
    {
        return orders
            .OrderBy(order => order.Id)
            .Select(order => new OrderRow(
                order.Id,
                order.Number,
                order.Customer.Name,
                order.Lines.Count));
    }

    public static IEnumerable<OrderRow> ForCustomerInMemory(IEnumerable<OrderRow> rows, string customerName)
    {
        return rows.Where(row => row.CustomerName == customerName);
    }
}
