using Microsoft.EntityFrameworkCore;

namespace LabEfCore.Api.Data;

public sealed class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<Order> Orders { get; set; } = [];
}

public sealed class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string Number { get; set; } = "";
    public string? Note { get; set; }
    public List<OrderLine> Lines { get; set; } = [];
}

public sealed class OrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public string Sku { get; set; } = "";
    public int Quantity { get; set; }
}

public sealed class SalesDbContext(DbContextOptions<SalesDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(customer =>
        {
            customer.Property(item => item.Name).HasMaxLength(120).IsRequired();
        });

        modelBuilder.Entity<Order>(order =>
        {
            order.Property(item => item.Number).HasMaxLength(32).IsRequired();
            order.HasIndex(item => item.Number).IsUnique();
            order.HasOne(item => item.Customer)
                .WithMany(customer => customer.Orders)
                .HasForeignKey(item => item.CustomerId);
            order.HasMany(item => item.Lines)
                .WithOne(line => line.Order)
                .HasForeignKey(line => line.OrderId);
        });

        modelBuilder.Entity<OrderLine>(line =>
        {
            line.Property(item => item.Sku).HasMaxLength(40).IsRequired();
        });
    }
}
