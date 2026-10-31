// MCP server that exposes the GraphQL API to an AI agent as two tools:
//
//   graphql_schema  - returns the API's schema (SDL) so the agent can read the
//                     contract before writing any query.
//   graphql_query   - validates and executes one GraphQL query against the API
//                     and returns the JSON result, including GraphQL errors, so
//                     the agent can self-correct and retry.
//
// Typical agent loop: schema -> draft query -> execute -> fix errors -> answer.
//
// Run the API first (npm run api), then this server (npm run mcp).
// Point any MCP client (Copilot CLI, Claude Code, ...) at:  node/tsx src/mcp.ts

import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { getIntrospectionQuery, buildClientSchema, printSchema } from "graphql";
import { z } from "zod";

const API_URL = process.env.GRAPHQL_URL ?? "http://localhost:4000";

async function callApi(query: string, variables?: Record<string, unknown>) {
  const res = await fetch(API_URL, {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ query, variables }),
  });
  if (!res.ok) throw new Error(`GraphQL API returned HTTP ${res.status}`);
  return res.json();
}

const server = new McpServer({ name: "graphql-orders", version: "1.0.0" });

server.tool(
  "graphql_schema",
  "Return the GraphQL schema (SDL) for the orders API. Read this BEFORE writing a query: " +
    "it lists every type, field, argument and description the API supports.",
  {},
  async () => {
    const json = await callApi(getIntrospectionQuery());
    if (json.errors) throw new Error(JSON.stringify(json.errors));
    const sdl = printSchema(buildClientSchema(json.data));
    return { content: [{ type: "text", text: sdl }] };
  }
);

server.tool(
  "graphql_query",
  "Execute ONE GraphQL query against the orders API and return the JSON result. " +
    "If the query is invalid, the result contains GraphQL errors describing exactly what is wrong - " +
    "fix the query and call again. Read-only: mutations are not supported by this API.",
  {
    query: z.string().describe("The GraphQL query document, e.g. { customers { nodes { name } } }"),
    variables: z
      .record(z.unknown())
      .optional()
      .describe("Optional variables object for the query"),
  },
  async ({ query, variables }) => {
    const json = await callApi(query, variables);
    return {
      content: [{ type: "text", text: JSON.stringify(json, null, 2) }],
      isError: Boolean(json.errors),
    };
  }
);

await server.connect(new StdioServerTransport());
console.error(`graphql-orders MCP server running (API: ${API_URL})`);
