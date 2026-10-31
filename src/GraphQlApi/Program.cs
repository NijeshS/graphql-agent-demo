// GraphQL API (HotChocolate) designed to be read by AI agents.
// Guardrail: a query depth limit is enforced server-side (AddMaxExecutionDepthRule),
// so an agent cannot run an arbitrarily expensive nested query.
//
// Run:  dotnet run --project src/GraphQlApi     ->  http://localhost:4000/graphql
// SDL:  GET http://localhost:4000/graphql/schema  (used by the MCP graphql_schema tool)

using GraphQlApi;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:4000");

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddTypeExtension<CustomerExtensions>()
    .AddTypeExtension<OrderExtensions>()
    .AddTypeExtension<InvoiceExtensions>()
    .AddMaxExecutionDepthRule(8) // guardrail: cap query depth
    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true); // demo: richer errors help agents self-correct

var app = builder.Build();

app.MapGraphQL();
app.MapGraphQLSchema(); // serves the SDL at /graphql/schema

app.Run();
