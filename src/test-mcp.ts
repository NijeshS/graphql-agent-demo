// Smoke test for the MCP server: lists its tools, reads the schema, runs the
// demo question as one GraphQL query, and shows that an invalid query comes
// back with errors an agent can self-correct from.
// Requires the API to be running (npm run api).  Run:  npm run test:mcp

import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { StdioClientTransport } from "@modelcontextprotocol/sdk/client/stdio.js";

const transport = new StdioClientTransport({
  command: "npx",
  args: ["tsx", "src/mcp.ts"],
});
const client = new Client({ name: "demo-test", version: "1.0.0" });
await client.connect(transport);

const tools = await client.listTools();
console.log("Tools:", tools.tools.map((t) => t.name).join(", "));

const schema = await client.callTool({ name: "graphql_schema", arguments: {} });
const sdl = (schema.content as { text: string }[])[0].text;
console.log(`\nSchema received: ${sdl.split("\n").length} lines of SDL`);

const good = await client.callTool({
  name: "graphql_query",
  arguments: {
    query: `{
      topCustomersByRevenue(limit: 5, fromDate: "2026-10-01", toDate: "2026-12-31") {
        revenue
        customer {
          name
          invoices(status: UNPAID) { id amount dueDate }
        }
      }
    }`,
  },
});
console.log("\nDemo question result:");
console.log((good.content as { text: string }[])[0].text);

const bad = await client.callTool({
  name: "graphql_query",
  arguments: { query: "{ customers { nodes { name, totalSpent } } }" },
});
console.log("\nInvalid query (note the self-correctable error):");
console.log((bad.content as { text: string }[])[0].text);

await client.close();
process.exit(0);
