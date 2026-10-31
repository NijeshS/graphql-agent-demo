# GraphQL as the API Layer for AI Agents — Demo (.NET)

Companion demo for the tech session **"GraphQL as the API Layer for AI Agents."**

REST forces an AI agent to guess endpoints, over-fetch, and stitch responses together across many calls. A GraphQL schema is a typed, self-describing contract: the agent can *read* it, then compose exactly the query it needs — one round trip. This demo makes that contrast concrete, and shows the guardrails that make it safe in production.

> This is the C#/.NET port of the original TypeScript demo (same schema, same seed data, same results). The TypeScript version is on the `main` branch history.

## Stack

- **GraphQL API** — ASP.NET Core + [HotChocolate](https://chillicream.com/docs/hotchocolate) 16, annotation-based types with agent-readable descriptions
- **MCP server** — console app on the official [Model Context Protocol C# SDK](https://github.com/modelcontextprotocol/csharp-sdk), stdio transport
- **REST contrast API** — ASP.NET Core minimal APIs over the same data
- **Shared** — models + seed data used by both APIs

## What's inside

```
GraphQLAgentDemo.sln
src/
  Shared/       Models + seed data: 8 customers, 15 orders, 14 invoices (Q4 2026)
  GraphQlApi/   HotChocolate API — described schema, cursor pagination, depth-limit guardrail
  McpServer/    MCP server exposing the API to an agent as two tools
  RestApi/      The same data as REST — for the contrast demo
tests/
  McpTest/      Smoke test: schema read + demo query + self-correction on a bad query
```

**Agent loop** (via MCP): `graphql_schema` → draft query → `graphql_query` → on error, read the GraphQL error, fix, retry.

## Run it

Requires the .NET 8 SDK (or later).

```bash
# Terminal 1 — GraphQL API on http://localhost:4000/graphql
dotnet run --project src/GraphQlApi

# Terminal 2 — smoke test the MCP server end to end (from the repo root)
dotnet run --project tests/McpTest

# Optional — REST API on http://localhost:4100 for the contrast demo
dotnet run --project src/RestApi
```

The schema SDL is served at `GET http://localhost:4000/graphql/schema` — that's what the MCP `graphql_schema` tool returns to the agent.

## Connect a real agent

Build the MCP server once (`dotnet build src/McpServer`), then add it to your agent's MCP config (Copilot CLI, Claude Code, etc.):

```json
{
  "mcpServers": {
    "graphql-orders": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/graphql-agent-demo/src/McpServer"]
    }
  }
}
```

(Keep the GraphQL API running — the MCP server calls it over HTTP. Set `GRAPHQL_URL` if the API runs elsewhere.)

Then ask, in plain English:

> "Top 5 customers by revenue this quarter, with their unpaid invoices."

The agent introspects the schema, finds `topCustomersByRevenue`, and answers with **one** query:

```graphql
{
  topCustomersByRevenue(limit: 5, fromDate: "2026-10-01", toDate: "2026-12-31") {
    revenue
    customer {
      name
      invoices(status: UNPAID) { id amount dueDate }
    }
  }
}
```

Against the REST API the same question takes 4+ calls (`/orders`, group and sum client-side, `/customers/:id`, `/customers/:id/invoices` …) and several full payloads of fields nobody asked for.

## Schema design rules for agents (as used in `GraphQlApi/Types.cs`)

- **Describe everything.** Every query, field, and argument has a plain-English `[GraphQLDescription]` — the agent reads these to pick the right field.
- **No ambiguous fields.** Explicit filters (`status`), explicit ranges (`fromDate`/`toDate`) instead of overloaded arguments.
- **Server-side aggregates for common questions.** `topCustomersByRevenue` and `outstandingBalance` exist so the agent doesn't fetch raw rows and aggregate (badly) in context.
- **Cursor pagination** (`first`/`after` + `endCursor`/`hasNextPage`) so agents page predictably instead of guessing offsets.

## Guardrails

| Guardrail | In this demo | In production |
|---|---|---|
| Query depth limit | `AddMaxExecutionDepthRule(8)` (GraphQlApi/Program.cs) | Also add query *cost* scoring |
| Read-only surface | API exposes queries only — no mutations | Persisted/allowlisted queries for agent identities |
| Field authorization | Not implemented (single demo dataset) | Scope fields by the agent's token; schema differs per role |
| Introspection / SDL | On (internal demo) | Off or restricted on public endpoints |
| Rate limits / audit | — | Per-agent-identity limits; log every query + cost + agent id |

## Session flow (45–60 min)

1. **Agents + REST fail live** — ask the demo question against the REST API.
2. **GraphQL as a contract** — walk the schema descriptions in `GraphQlApi/Types.cs`.
3. **MCP wiring** — show the two tools in `McpServer/GraphQlTools.cs`; agent loop: read schema → draft → execute → self-correct.
4. **The demo** — the question above, then a deliberately ambiguous ask to watch validation + self-correction.
5. **Guardrails** — trip the depth limit live with a deeply nested query; walk the table above.
6. **When not to use it** — simple CRUD, streaming data, third-party APIs you don't control.
