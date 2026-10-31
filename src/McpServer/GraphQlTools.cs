// MCP tools that expose the GraphQL API to an AI agent:
//
//   graphql_schema  - returns the API's schema (SDL) so the agent can read the
//                     contract before writing any query.
//   graphql_query   - executes one GraphQL query against the API and returns
//                     the JSON result, including GraphQL errors, so the agent
//                     can self-correct and retry.
//
// Typical agent loop: schema -> draft query -> execute -> fix errors -> answer.

using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class GraphQlTools
{
    private static readonly HttpClient Http = new();

    private static string ApiUrl =>
        Environment.GetEnvironmentVariable("GRAPHQL_URL") ?? "http://localhost:4000/graphql";

    [McpServerTool(Name = "graphql_schema")]
    [Description("Return the GraphQL schema (SDL) for the orders API. Read this BEFORE writing a query: it lists every type, field, argument and description the API supports.")]
    public static async Task<string> GetSchema(CancellationToken cancellationToken)
    {
        var res = await Http.GetAsync(ApiUrl.TrimEnd('/') + "/schema", cancellationToken);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsStringAsync(cancellationToken);
    }

    [McpServerTool(Name = "graphql_query")]
    [Description("Execute ONE GraphQL query against the orders API and return the JSON result. If the query is invalid, the result contains GraphQL errors describing exactly what is wrong - fix the query and call again. Read-only: mutations are not supported by this API.")]
    public static async Task<CallToolResult> ExecuteQuery(
        [Description("The GraphQL query document, e.g. { customers { nodes { name } } }")] string query,
        [Description("Optional variables for the query, as a JSON object string")] string? variables = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object?> { ["query"] = query };
        if (!string.IsNullOrWhiteSpace(variables))
            payload["variables"] = JsonSerializer.Deserialize<JsonElement>(variables);

        var res = await Http.PostAsync(
            ApiUrl,
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            cancellationToken);
        var text = await res.Content.ReadAsStringAsync(cancellationToken);

        var pretty = text;
        try
        {
            pretty = JsonSerializer.Serialize(
                JsonSerializer.Deserialize<JsonElement>(text),
                new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException) { /* leave the raw body as-is */ }

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = pretty }],
            IsError = text.Contains("\"errors\"")
        };
    }
}
