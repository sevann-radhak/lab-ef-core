using Microsoft.EntityFrameworkCore;

namespace LabEfCore.Api.Data;

public static class SalesSeed
{
    public static async Task ApplyAsync(SalesDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        if (await db.Customers.AnyAsync())
        {
            return;
        }

        Customer[] customers =
        [
            new() { Name = "Ada Bakery" },
            new() { Name = "Lin Roasters" },
            new() { Name = "Mei Market" },
        ];

        db.Customers.AddRange(customers);
        db.Orders.AddRange(
            Order(customers[0], "ORD-1001", "flour", "butter"),
            Order(customers[0], "ORD-1002", "yeast"),
            Order(customers[1], "ORD-1003", "beans"),
            Order(customers[2], "ORD-1004", "rice", "tea"));
        await db.SaveChangesAsync();
    }

    private static Order Order(Customer customer, string number, params string[] skus)
    {
        return new Order
        {
            Customer = customer,
            Number = number,
            Lines = skus.Select(sku => new OrderLine { Sku = sku, Quantity = 1 }).ToList(),
        };
    }
}
