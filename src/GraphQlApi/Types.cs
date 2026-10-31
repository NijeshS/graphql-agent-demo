using Shared;

namespace GraphQlApi;

// GraphQL types designed to be read by AI agents:
// - Plain-English descriptions on fields and arguments (agents read these).
// - Explicit filters, cursor pagination, no ambiguous overloaded fields.
// - Server-side aggregates (TopCustomersByRevenue, OutstandingBalance) so the
//   agent does not fetch raw rows and aggregate in context.

public class Query
{
    [GraphQLDescription("List customers. Use 'first'/'after' for cursor pagination; results are ordered by name.")]
    public CustomerConnection GetCustomers(
        [GraphQLDescription("Max number of customers to return (default 20, max 50).")] int first = 20,
        [GraphQLDescription("Cursor from a previous page's endCursor.")] string? after = null)
    {
        first = Math.Clamp(first, 1, 50);
        var sorted = Store.Customers.OrderBy(c => c.Name).ToList();
        var start = after is null ? 0 : sorted.FindIndex(c => c.Id == after) + 1;
        var page = sorted.Skip(start).Take(first).ToList();
        return new CustomerConnection
        {
            Nodes = page,
            EndCursor = page.Count > 0 ? page[^1].Id : null,
            HasNextPage = start + first < sorted.Count
        };
    }

    [GraphQLDescription("Fetch one customer by id, e.g. 'c1'.")]
    public Customer? GetCustomer(string id) => Store.CustomerById(id);

    [GraphQLDescription("Customers ranked by revenue (sum of non-cancelled order amounts) within an optional date range. Use this for 'top customers by revenue' questions instead of aggregating client-side.")]
    public IEnumerable<CustomerRevenue> GetTopCustomersByRevenue(
        [GraphQLDescription("How many customers to return (default 5).")] int limit = 5,
        [GraphQLDescription("Only count orders created on/after this ISO date.")] string? fromDate = null,
        [GraphQLDescription("Only count orders created on/before this ISO date.")] string? toDate = null)
    {
        limit = Math.Clamp(limit, 1, 50);
        bool InRange(Order o) =>
            o.Status != OrderStatus.Cancelled &&
            (fromDate is null || string.CompareOrdinal(o.CreatedAt, fromDate) >= 0) &&
            (toDate is null || string.CompareOrdinal(o.CreatedAt, toDate) <= 0);

        return Store.Customers
            .Select(c => new CustomerRevenue
            {
                Customer = c,
                Revenue = Store.OrdersOf(c.Id).Where(InRange).Sum(o => o.Amount)
            })
            .Where(r => r.Revenue > 0)
            .OrderByDescending(r => r.Revenue)
            .Take(limit);
    }

    [GraphQLDescription("Invoices that still need payment. Defaults to UNPAID and OVERDUE.")]
    public IEnumerable<Invoice> GetUnpaidInvoices(
        [GraphQLDescription("Restrict to one status: UNPAID or OVERDUE. Omit for both.")] InvoiceStatus? status = null)
    {
        var wanted = status is { } s ? new[] { s } : new[] { InvoiceStatus.Unpaid, InvoiceStatus.Overdue };
        return Store.Invoices.Where(i => wanted.Contains(i.Status));
    }

    [GraphQLDescription("Fetch one order by id.")]
    public Order? GetOrder(string id) => Store.OrderById(id);
}

[GraphQLDescription("A page of customers plus the cursor to fetch the next page.")]
public class CustomerConnection
{
    public IReadOnlyList<Customer> Nodes { get; set; } = Array.Empty<Customer>();

    [GraphQLDescription("Pass this as 'after' to get the next page; null when there are no more pages.")]
    public string? EndCursor { get; set; }

    [GraphQLDescription("True when another page exists after this one.")]
    public bool HasNextPage { get; set; }
}

[GraphQLDescription("A customer together with their revenue in the requested period.")]
public class CustomerRevenue
{
    public Customer Customer { get; set; } = default!;

    [GraphQLDescription("Revenue in USD for the requested period.")]
    public decimal Revenue { get; set; }
}

[ExtendObjectType(typeof(Customer))]
public class CustomerExtensions
{
    [GraphQLDescription("All orders placed by this customer, newest first.")]
    public IEnumerable<Order> GetOrders(
        [Parent] Customer customer,
        [GraphQLDescription("Optional order status filter: FULFILLED, PENDING or CANCELLED.")] OrderStatus? status = null) =>
        Store.OrdersOf(customer.Id)
            .Where(o => status is null || o.Status == status)
            .OrderByDescending(o => o.CreatedAt);

    [GraphQLDescription("All invoices issued to this customer, across all their orders.")]
    public IEnumerable<Invoice> GetInvoices(
        [Parent] Customer customer,
        [GraphQLDescription("Optional invoice status filter: PAID, UNPAID or OVERDUE.")] InvoiceStatus? status = null) =>
        Store.InvoicesOfCustomer(customer.Id)
            .Where(i => status is null || i.Status == status);

    [GraphQLDescription("Lifetime revenue in USD: sum of this customer's non-cancelled order amounts.")]
    public decimal GetTotalRevenue([Parent] Customer customer) =>
        Store.OrdersOf(customer.Id)
            .Where(o => o.Status != OrderStatus.Cancelled)
            .Sum(o => o.Amount);

    [GraphQLDescription("Amount in USD currently owed by this customer (UNPAID + OVERDUE invoices).")]
    public decimal GetOutstandingBalance([Parent] Customer customer) =>
        Store.InvoicesOfCustomer(customer.Id)
            .Where(i => i.Status is InvoiceStatus.Unpaid or InvoiceStatus.Overdue)
            .Sum(i => i.Amount);
}

[ExtendObjectType(typeof(Order))]
public class OrderExtensions
{
    [GraphQLDescription("The customer who placed this order.")]
    public Customer? GetCustomer([Parent] Order order) => Store.CustomerById(order.CustomerId);

    [GraphQLDescription("Invoices issued for this order (usually exactly one).")]
    public IEnumerable<Invoice> GetInvoices([Parent] Order order) => Store.InvoicesOfOrder(order.Id);
}

[ExtendObjectType(typeof(Invoice))]
public class InvoiceExtensions
{
    [GraphQLDescription("The order this invoice bills.")]
    public Order? GetOrder([Parent] Invoice invoice) => Store.OrderById(invoice.OrderId);
}
