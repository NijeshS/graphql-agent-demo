// Smoke test for the MCP server: exercises its GraphQL and REST tools.
// Requires both APIs to be running (src/GraphQlApi and src/RestApi).
// Run from the repo root:  dotnet run --project tests/McpTest

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

var transport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "graphql-orders",
    Command = "dotnet",
    Arguments = ["run", "--project", "src/McpServer/McpServer.csproj"],
    WorkingDirectory = Directory.GetCurrentDirectory()
});

await using var client = await McpClient.CreateAsync(transport);

var tools = await client.ListToolsAsync();
Console.WriteLine("Tools: " + string.Join(", ", tools.Select(t => t.Name)));
var toolNames = tools.Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
foreach (var expected in new[]
{
    "graphql_schema", "graphql_query",
    "rest_customers", "rest_customer", "rest_customer_orders",
    "rest_customer_invoices", "rest_orders", "rest_invoices"
})
{
    if (!toolNames.Contains(expected))
        throw new InvalidOperationException($"Expected MCP tool '{expected}' to be registered.");
}

string TextOf(CallToolResult r) =>
    string.Concat(r.Content.OfType<TextContentBlock>().Select(b => b.Text));

var schema = await client.CallToolAsync("graphql_schema", new Dictionary<string, object?>());
Console.WriteLine($"\nSchema received: {TextOf(schema).Split('\n').Length} lines of SDL");

var good = await client.CallToolAsync("graphql_query", new Dictionary<string, object?>
{
    ["query"] = """
        {
          topCustomersByRevenue(limit: 5, fromDate: "2026-10-01", toDate: "2026-12-31") {
            revenue
            customer {
              name
              invoices(status: UNPAID) { id amount dueDate }
            }
          }
        }
        """
});
Console.WriteLine("\nDemo question result:");
Console.WriteLine(TextOf(good));

var bad = await client.CallToolAsync("graphql_query", new Dictionary<string, object?>
{
    ["query"] = "{ customers { nodes { name totalSpent } } }"
});
Console.WriteLine("\nInvalid query (note the self-correctable error):");
Console.WriteLine(TextOf(bad));

var restCustomer = await client.CallToolAsync("rest_customer", new Dictionary<string, object?>
{
    ["id"] = "c1"
});
AssertContains(TextOf(restCustomer), "Acme Retail Group", "REST customer lookup");

var restCustomers = await client.CallToolAsync("rest_customers", new Dictionary<string, object?>());
AssertContains(TextOf(restCustomers), "Acme Retail Group", "REST customer list");

var restOrders = await client.CallToolAsync("rest_customer_orders", new Dictionary<string, object?>
{
    ["id"] = "c1"
});
AssertContains(TextOf(restOrders), "o1001", "REST customer orders");

var allOrders = await client.CallToolAsync("rest_orders", new Dictionary<string, object?>());
AssertContains(TextOf(allOrders), "o1001", "REST order list");

var restInvoices = await client.CallToolAsync("rest_customer_invoices", new Dictionary<string, object?>
{
    ["id"] = "c1"
});
AssertContains(TextOf(restInvoices), "inv-502", "REST customer invoices");

var filteredInvoices = await client.CallToolAsync("rest_invoices", new Dictionary<string, object?>
{
    ["status"] = "OVERDUE"
});
AssertContains(TextOf(filteredInvoices), "inv-507", "REST invoice status filter");
AssertContains(TextOf(filteredInvoices), "HTTP 200", "REST invoice status filter response");
Console.WriteLine("\nREST MCP tools returned customer, order, invoice, and filtered invoice data.");

static void AssertContains(string actual, string expected, string scenario)
{
    if (!actual.Contains(expected, StringComparison.Ordinal))
        throw new InvalidOperationException($"{scenario} did not contain expected value '{expected}'.\n{actual}");
}
