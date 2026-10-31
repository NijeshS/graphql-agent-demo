# GraphQL as the API Layer for AI Agents — Demo

Companion demo for the tech session **"GraphQL as the API Layer for AI Agents."**

REST forces an AI agent to guess endpoints, over-fetch, and stitch responses together across many calls. A GraphQL schema is a typed, self-describing contract: the agent can *read* it, then compose exactly the query it needs — one round trip. This demo makes that contrast concrete, and shows the guardrails that make it safe in production.

## What's inside

```
src/
  data.ts     Seed data: 8 customers, 15 orders, 14 invoices (Q4 2026)
  server.ts   GraphQL API (Apollo Server) — agent-readable schema, depth-limit guardrail
  mcp.ts      MCP server exposing the API to an agent as two tools
  rest.ts     The same data as REST — for the contrast demo
  test-mcp.ts Smoke test: schema read + demo query + self-correction on a bad query
```

**Agent loop** (via MCP): `graphql_schema` → draft query → `graphql_query` → on error, read the GraphQL error, fix, retry.

## Run it

```bash
npm install

# Terminal 1 — GraphQL API on http://localhost:4000
npm run api

# Terminal 2 — smoke test the MCP server end to end
npm run test:mcp

# Optional — REST API on http://localhost:4100 for the contrast demo
npm run rest
```

## Connect a real agent

Add the MCP server to your agent's MCP config (Copilot CLI, Claude Code, etc.):

```json
{
  "mcpServers": {
    "graphql-orders": {
      "command": "npx",
      "args": ["tsx", "src/mcp.ts"],
      "cwd": "/path/to/graphql-agent-demo"
    }
  }
}
```

(Keep `npm run api` running — the MCP server calls the API over HTTP. Set `GRAPHQL_URL` if the API runs elsewhere.)

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

## Schema design rules for agents (as used in `server.ts`)

- **Describe everything.** Every type, field, and argument has a plain-English description — the agent reads these to pick the right field.
- **No ambiguous fields.** Explicit filters (`status`), explicit ranges (`fromDate`/`toDate`) instead of overloaded arguments.
- **Server-side aggregates for common questions.** `topCustomersByRevenue` and `outstandingBalance` exist so the agent doesn't fetch raw rows and aggregate (badly) in context.
- **Cursor pagination** (`first`/`after` + `endCursor`/`hasNextPage`) so agents page predictably instead of guessing offsets.

## Guardrails

| Guardrail | In this demo | In production |
|---|---|---|
| Query depth limit | `graphql-depth-limit`, max depth 8 (server.ts) | Also add query *cost* scoring (e.g. graphql-query-complexity) |
| Read-only surface | API exposes queries only — no mutations | Persisted/allowlisted queries for agent identities |
| Field authorization | Not implemented (single demo dataset) | Scope fields by the agent's token; schema differs per role |
| Introspection | On (internal demo) | Off or restricted on public endpoints |
| Rate limits / audit | — | Per-agent-identity limits; log every query + cost + agent id |

## Session flow (45–60 min)

1. **Agents + REST fail live** — ask the demo question against `npm run rest`.
2. **GraphQL as a contract** — walk the schema descriptions in `server.ts`.
3. **MCP wiring** — show the two tools in `mcp.ts`; agent loop: read schema → draft → execute → self-correct.
4. **The demo** — the question above, then a deliberately ambiguous ask to watch validation + self-correction.
5. **Guardrails** — trip the depth limit live with a deeply nested query; walk the table above.
6. **When not to use it** — simple CRUD, streaming data, third-party APIs you don't control.
