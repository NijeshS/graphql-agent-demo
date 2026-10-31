// MCP server host (stdio transport). Tools live in GraphQlTools.cs.
// Run the API first (dotnet run --project src/GraphQlApi), then this server:
//   dotnet run --project src/McpServer
// Point any MCP client (Copilot CLI, Claude Code, ...) at this project.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// MCP servers on stdio must keep stdout clean for JSON-RPC: log to stderr.
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
