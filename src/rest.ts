// The same data as a plain REST API - used in the session for contrast.
// Answering "top 5 customers by revenue this quarter, with their unpaid
// invoices" takes several calls here (list orders -> group client-side ->
// fetch each customer -> fetch each customer's invoices), while the GraphQL
// API answers it with one query (see README).
//
// Run:  npm run rest    ->  http://localhost:4100

import { createServer } from "node:http";
import { customers, orders, invoices, customerById, orderById } from "./data.js";

const server = createServer((req, res) => {
  const url = new URL(req.url ?? "/", "http://localhost:4100");
  const send = (code: number, body: unknown) => {
    res.writeHead(code, { "content-type": "application/json" });
    res.end(JSON.stringify(body));
  };
  const parts = url.pathname.split("/").filter(Boolean);

  if (url.pathname === "/customers") return send(200, customers);
  if (parts[0] === "customers" && parts[1] && !parts[2]) {
    const c = customerById(parts[1]);
    return c ? send(200, c) : send(404, { error: "customer not found" });
  }
  if (parts[0] === "customers" && parts[2] === "orders") {
    return send(200, orders.filter((o) => o.customerId === parts[1]));
  }
  if (parts[0] === "customers" && parts[2] === "invoices") {
    return send(
      200,
      invoices.filter((i) => orderById(i.orderId)?.customerId === parts[1])
    );
  }
  if (url.pathname === "/orders") return send(200, orders);
  if (url.pathname === "/invoices") {
    const status = url.searchParams.get("status");
    return send(200, status ? invoices.filter((i) => i.status === status) : invoices);
  }
  return send(404, { error: "not found" });
});

server.listen(4100, () => console.log("REST API ready at http://localhost:4100"));
