// Smoke test for the MCP server: lists its tools, reads the schema, runs the
// demo question as one GraphQL query, and shows that an invalid query comes
// back with errors an agent can self-correct from.
// Requires the API to be running (dotnet run --project src/GraphQlApi).
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
