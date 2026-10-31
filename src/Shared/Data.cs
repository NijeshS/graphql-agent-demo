namespace Shared;

// Seed data for the demo: customers, their orders, and invoices.
// Dates sit in Q4 2026 so "this quarter" questions work during the session.
// Same dataset as the original TypeScript version.

public enum OrderStatus
{
    Fulfilled,
    Pending,
    Cancelled
}

public enum InvoiceStatus
{
    Paid,
    Unpaid,
    Overdue
}

public record Customer(string Id, string Name, string Email, string Country, string SignedUp);

public record Order(string Id, string CustomerId, decimal Amount, OrderStatus Status, string CreatedAt);

public record Invoice(string Id, string OrderId, decimal Amount, InvoiceStatus Status, string DueDate);

public static class Store
{
    public static readonly IReadOnlyList<Customer> Customers = new List<Customer>
    {
        new("c1", "Acme Retail Group", "ap@acmeretail.example", "US", "2024-03-11"),
        new("c2", "Northwind Traders", "billing@northwind.example", "US", "2023-08-02"),
        new("c3", "Globex GmbH", "finance@globex.example", "DE", "2024-01-25"),
        new("c4", "Initech LLC", "accounts@initech.example", "US", "2025-05-19"),
        new("c5", "Umbrella Health", "payables@umbrella.example", "UK", "2023-11-30"),
        new("c6", "Stark Components", "ap@starkcomponents.example", "US", "2025-02-14"),
        new("c7", "Wayne Logistics", "finance@waynelogistics.example", "CA", "2024-07-08"),
        new("c8", "Soylent Foods", "billing@soylent.example", "US", "2025-09-21"),
    };

    public static readonly IReadOnlyList<Order> Orders = new List<Order>
    {
        new("o1001", "c1", 12400m, OrderStatus.Fulfilled, "2026-10-03"),
        new("o1002", "c1", 8300m, OrderStatus.Fulfilled, "2026-10-21"),
        new("o1003", "c1", 19750m, OrderStatus.Fulfilled, "2026-11-14"),
        new("o1004", "c2", 22100m, OrderStatus.Fulfilled, "2026-10-09"),
        new("o1005", "c2", 6400m, OrderStatus.Pending, "2026-11-28"),
        new("o1006", "c3", 31200m, OrderStatus.Fulfilled, "2026-10-17"),
        new("o1007", "c3", 9800m, OrderStatus.Fulfilled, "2026-11-05"),
        new("o1008", "c4", 4300m, OrderStatus.Fulfilled, "2026-10-26"),
        new("o1009", "c5", 27600m, OrderStatus.Fulfilled, "2026-11-11"),
        new("o1010", "c5", 11200m, OrderStatus.Fulfilled, "2026-10-30"),
        new("o1011", "c6", 15800m, OrderStatus.Fulfilled, "2026-11-19"),
        new("o1012", "c6", 2900m, OrderStatus.Cancelled, "2026-11-22"),
        new("o1013", "c7", 9100m, OrderStatus.Fulfilled, "2026-10-13"),
        new("o1014", "c8", 5600m, OrderStatus.Fulfilled, "2026-11-25"),
        new("o1015", "c2", 17400m, OrderStatus.Fulfilled, "2026-08-15"), // Q3 - outside "this quarter"
    };

    public static readonly IReadOnlyList<Invoice> Invoices = new List<Invoice>
    {
        new("inv-501", "o1001", 12400m, InvoiceStatus.Paid, "2026-11-02"),
        new("inv-502", "o1002", 8300m, InvoiceStatus.Unpaid, "2026-11-20"),
        new("inv-503", "o1003", 19750m, InvoiceStatus.Unpaid, "2026-12-14"),
        new("inv-504", "o1004", 22100m, InvoiceStatus.Paid, "2026-11-08"),
        new("inv-505", "o1005", 6400m, InvoiceStatus.Unpaid, "2026-12-28"),
        new("inv-506", "o1006", 31200m, InvoiceStatus.Paid, "2026-11-16"),
        new("inv-507", "o1007", 9800m, InvoiceStatus.Overdue, "2026-12-05"),
        new("inv-508", "o1008", 4300m, InvoiceStatus.Paid, "2026-11-25"),
        new("inv-509", "o1009", 27600m, InvoiceStatus.Unpaid, "2026-12-11"),
        new("inv-510", "o1010", 11200m, InvoiceStatus.Overdue, "2026-11-29"),
        new("inv-511", "o1011", 15800m, InvoiceStatus.Paid, "2026-12-19"),
        new("inv-512", "o1013", 9100m, InvoiceStatus.Paid, "2026-11-12"),
        new("inv-513", "o1014", 5600m, InvoiceStatus.Unpaid, "2026-12-25"),
        new("inv-514", "o1015", 17400m, InvoiceStatus.Paid, "2026-09-14"),
    };

    public static Customer? CustomerById(string id) => Customers.FirstOrDefault(c => c.Id == id);
    public static Order? OrderById(string id) => Orders.FirstOrDefault(o => o.Id == id);
    public static IEnumerable<Order> OrdersOf(string customerId) => Orders.Where(o => o.CustomerId == customerId);
    public static IEnumerable<Invoice> InvoicesOfOrder(string orderId) => Invoices.Where(i => i.OrderId == orderId);
    public static IEnumerable<Invoice> InvoicesOfCustomer(string customerId) =>
        Invoices.Where(i => OrderById(i.OrderId)?.CustomerId == customerId);
}
