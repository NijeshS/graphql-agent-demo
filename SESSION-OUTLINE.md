# Tech Session: GraphQL as the API Layer for AI Agents

**Format:** 45–60 minutes, mostly live demo
**Audience:** Backend/full-stack developers, platform team
**One-line pitch:** REST forces AI agents to guess endpoints and over-fetch. A GraphQL schema is a typed, self-describing contract — an agent can introspect it and ask for exactly the data it needs, in one call. Here's how to expose that safely in production.

## Why this matters

- Agents calling REST waste tokens and round trips: endpoint discovery, over-fetching, stitching responses client-side.
- GraphQL schema = machine-readable contract: types, fields, pagination, deprecations — everything an agent (or an MCP tool) needs to generate correct calls.
- The gateway is where this lands in production: one governed entry point for agents, with auth, cost limits, and observability — the same pattern as AgentCore Gateway / MCP setups.

## Agenda

### 1. The problem: agents + REST (5 min)
- Live fail: ask a coding agent a question ("which customers ordered X and haven't paid?") against a REST API — watch it chain 4–5 calls, over-fetch, and burn context.
- Cost/latency/context comparison vs. a single composed query.

### 2. GraphQL as an agent-native contract (10 min)
- Introspection: how an agent explores the schema before writing a query.
- Why typed fields + explicit pagination beat endpoint guessing.
- Schema design rules for agents:
  - Descriptive names and descriptions on fields (the agent reads them).
  - No ambiguous/overloaded fields; explicit filters and sort arguments.
  - Stable pagination (cursor-based) so agents can page predictably.
  - Deprecations with guidance, not silent removals.

### 3. Connecting the agent: GraphQL through MCP (10 min)
- Wrap the GraphQL endpoint as an MCP server (one `query` tool, schema-aware).
- Agent workflow: introspect → draft query → validate → execute → self-correct on errors.
- Demo architecture: Agent (Copilot CLI / Claude Code / Strands) → MCP server → GraphQL API → services/DB.

### 4. The demo (15 min)
Setup: a small orders/customers GraphQL API (Apollo Server or AWS AppSync) + an MCP wrapper.
1. Ask in plain English: "Top 5 customers by revenue this quarter, with their unpaid invoices."
2. Show the agent introspecting the schema and composing one query.
3. Run it; show a single round trip returning exactly the needed fields.
4. Break it on purpose: ask for something ambiguous → show the validation error and the agent's self-correction.
5. Show the same question against REST for contrast (calls made, payload size).

### 5. Guardrails — letting an agent write queries safely (10 min)
- **Query cost/depth limits:** reject expensive nested queries before they run (complexity scoring).
- **Persisted/allowlisted queries** for production agents where free-form queries are too risky.
- **Field-level authorization:** the agent's token scopes what fields it can see — schema looks different per role.
- **Introspection off (or restricted) in production** for public endpoints; on for trusted internal agents.
- **Rate limits + timeouts per agent identity**, and full query logging for audit (ties into observability: log the query, cost, and agent ID).
- Sandboxing angle: run the agent with network/credential policies (e.g., OpenShell) so it can only reach the gateway.

### 6. When NOT to use this (3 min)
- Simple CRUD with one consumer — REST is fine.
- Streaming/high-frequency data — subscriptions or purpose-built APIs.
- Public third-party APIs where you don't control the schema.

### 7. Takeaways + discussion (5 min)
- Design schemas like an agent will read them — because it will.
- One gateway, governed: auth, cost limits, logging.
- Start with read-only queries; add mutations behind allowlists later.
- Discussion: which of our internal APIs would benefit first?

## Prep checklist (presenter)
- [ ] Sample GraphQL API with seed data (orders, customers, invoices)
- [ ] MCP server wrapping the endpoint (introspection + execute tools)
- [ ] Agent CLI configured with the MCP server
- [ ] Cost-limit and persisted-query examples ready to toggle live
- [ ] REST version of the same API for the contrast demo
- [ ] One-page handout: schema-design rules for agents + guardrail checklist
