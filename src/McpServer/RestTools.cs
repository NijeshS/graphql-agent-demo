// MCP tools that expose the REST API alongside the GraphQL tools.

using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class RestTools
{
    private static readonly HttpClient Http = new();

    private static string ApiUrl =>
        Environment.GetEnvironmentVariable("REST_URL") ?? "http://localhost:4100";

    [McpServerTool(Name = "rest_customers")]
    [Description("List all customers from the REST API. Use this to compare REST results with the GraphQL API.")]
    public static Task<CallToolResult> GetCustomers(CancellationToken cancellationToken) =>
        GetAsync("/customers", cancellationToken);

    [McpServerTool(Name = "rest_customer")]
    [Description("Fetch one customer from the REST API by id, for example 'c1'.")]
    public static Task<CallToolResult> GetCustomer(
        [Description("The customer id, for example 'c1'.")] string id,
        CancellationToken cancellationToken) =>
        GetAsync($"/customers/{Uri.EscapeDataString(id)}", cancellationToken);

    [McpServerTool(Name = "rest_customer_orders")]
    [Description("List all orders for one customer from the REST API. This is a separate REST call from fetching the customer.")]
    public static Task<CallToolResult> GetCustomerOrders(
        [Description("The customer id, for example 'c1'.")] string id,
        CancellationToken cancellationToken) =>
        GetAsync($"/customers/{Uri.EscapeDataString(id)}/orders", cancellationToken);

    [McpServerTool(Name = "rest_customer_invoices")]
    [Description("List all invoices for one customer from the REST API. This is a separate REST call from fetching the customer.")]
    public static Task<CallToolResult> GetCustomerInvoices(
        [Description("The customer id, for example 'c1'.")] string id,
        CancellationToken cancellationToken) =>
        GetAsync($"/customers/{Uri.EscapeDataString(id)}/invoices", cancellationToken);

    [McpServerTool(Name = "rest_orders")]
    [Description("List all orders from the REST API.")]
    public static Task<CallToolResult> GetOrders(CancellationToken cancellationToken) =>
        GetAsync("/orders", cancellationToken);

    [McpServerTool(Name = "rest_invoices")]
    [Description("List invoices from the REST API. Optionally pass a status such as PAID, UNPAID, or OVERDUE to filter them.")]
    public static Task<CallToolResult> GetInvoices(
        [Description("Optional invoice status filter: PAID, UNPAID, or OVERDUE.")] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var path = string.IsNullOrWhiteSpace(status)
            ? "/invoices"
            : $"/invoices?status={Uri.EscapeDataString(status)}";
        return GetAsync(path, cancellationToken);
    }

    private static async Task<CallToolResult> GetAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await Http.GetAsync(ApiUrl.TrimEnd('/') + path, cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);

        var pretty = text;
        try
        {
            pretty = JsonSerializer.Serialize(
                JsonSerializer.Deserialize<JsonElement>(text),
                new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            // Keep non-JSON responses readable as-is.
        }

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = $"HTTP {(int)response.StatusCode}\n{pretty}" }],
            IsError = !response.IsSuccessStatusCode
        };
    }
}
