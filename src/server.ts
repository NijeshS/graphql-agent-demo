// GraphQL API designed to be read by AI agents:
// - Every type and field carries a plain-English description (agents read these).
// - Explicit filters, cursor pagination, no ambiguous overloaded fields.
// - Guardrail: a query depth limit is enforced server-side (see validationRules),
//   so an agent cannot run an arbitrarily expensive nested query.
//
// Run:  npm run api     ->  http://localhost:4000

import { ApolloServer } from "@apollo/server";
import { startStandaloneServer } from "@apollo/server/standalone";
import depthLimit from "graphql-depth-limit";
import {
  customers,
  orders,
  invoices,
  customerById,
  orderById,
  ordersOf,
  invoicesOfOrder,
  invoicesOfCustomer,
  type InvoiceStatus,
} from "./data.js";

const MAX_DEPTH = 8;

const typeDefs = /* GraphQL */ `
  "A company that buys from us."
  type Customer {
    "Unique customer id, e.g. 'c1'."
    id: ID!
    "Company name."
    name: String!
    "Billing contact email."
    email: String!
    "Two-letter country code."
    country: String!
    "Date the customer signed up (ISO date)."
    signedUp: String!
    "All orders placed by this customer, newest first."
    orders(
      "Optional order status filter: FULFILLED, PENDING or CANCELLED."
      status: OrderStatus
    ): [Order!]!
    "All invoices issued to this customer, across all their orders."
    invoices(
      "Optional invoice status filter: PAID, UNPAID or OVERDUE."
      status: InvoiceStatus
    ): [Invoice!]!
    "Lifetime revenue in USD: sum of this customer's non-cancelled order amounts."
    totalRevenue: Float!
    "Amount in USD currently owed by this customer (UNPAID + OVERDUE invoices)."
    outstandingBalance: Float!
  }

  "A single purchase order."
  type Order {
    id: ID!
    "Order amount in USD."
    amount: Float!
    status: OrderStatus!
    "Date the order was placed (ISO date)."
    createdAt: String!
    "The customer who placed this order."
    customer: Customer!
    "Invoices issued for this order (usually exactly one)."
    invoices: [Invoice!]!
  }

  enum OrderStatus {
    FULFILLED
    PENDING
    CANCELLED
  }

  "A bill issued for an order."
  type Invoice {
    id: ID!
    "Invoice amount in USD."
    amount: Float!
    "PAID = settled, UNPAID = open and not yet due or due soon, OVERDUE = past due date and unpaid."
    status: InvoiceStatus!
    "Payment due date (ISO date)."
    dueDate: String!
    "The order this invoice bills."
    order: Order!
  }

  enum InvoiceStatus {
    PAID
    UNPAID
    OVERDUE
  }

  type Query {
    "List customers. Use 'first'/'after' for cursor pagination; results are ordered by name."
    customers(
      "Max number of customers to return (default 20, max 50)."
      first: Int = 20
      "Cursor from a previous page's endCursor."
      after: String
    ): CustomerConnection!

    "Fetch one customer by id."
    customer("Customer id, e.g. 'c1'." id: ID!): Customer

    "Customers ranked by revenue (sum of non-cancelled order amounts) within an optional date range. Use this for 'top customers by revenue' questions instead of aggregating client-side."
    topCustomersByRevenue(
      "How many customers to return (default 5)."
      limit: Int = 5
      "Only count orders created on/after this ISO date."
      fromDate: String
      "Only count orders created on/before this ISO date."
      toDate: String
    ): [CustomerRevenue!]!

    "Invoices that still need payment. Defaults to UNPAID and OVERDUE."
    unpaidInvoices(
      "Restrict to one status: UNPAID or OVERDUE. Omit for both."
      status: InvoiceStatus
    ): [Invoice!]!

    "Fetch one order by id."
    order(id: ID!): Order
  }

  "A page of customers plus the cursor to fetch the next page."
  type CustomerConnection {
    nodes: [Customer!]!
    "Pass this as 'after' to get the next page; null when there are no more pages."
    endCursor: String
    "True when another page exists after this one."
    hasNextPage: Boolean!
  }

  "A customer together with their revenue in the requested period."
  type CustomerRevenue {
    customer: Customer!
    "Revenue in USD for the requested period."
    revenue: Float!
  }
`;

interface CustomerArgs {
  first?: number;
  after?: string;
}

const resolvers = {
  Query: {
    customers: (_: unknown, args: CustomerArgs) => {
      const first = Math.min(Math.max(args.first ?? 20, 1), 50);
      const sorted = [...customers].sort((a, b) => a.name.localeCompare(b.name));
      const start = args.after ? sorted.findIndex((c) => c.id === args.after) + 1 : 0;
      const page = sorted.slice(start, start + first);
      return {
        nodes: page,
        endCursor: page.length ? page[page.length - 1].id : null,
        hasNextPage: start + first < sorted.length,
      };
    },
    customer: (_: unknown, args: { id: string }) => customerById(args.id) ?? null,
    topCustomersByRevenue: (
      _: unknown,
      args: { limit?: number; fromDate?: string; toDate?: string }
    ) => {
      const limit = Math.min(Math.max(args.limit ?? 5, 1), 50);
      const inRange = (o: (typeof orders)[number]) =>
        o.status !== "CANCELLED" &&
        (!args.fromDate || o.createdAt >= args.fromDate) &&
        (!args.toDate || o.createdAt <= args.toDate);
      return customers
        .map((customer) => ({
          customer,
          revenue: orders
            .filter((o) => o.customerId === customer.id && inRange(o))
            .reduce((sum, o) => sum + o.amount, 0),
        }))
        .filter((r) => r.revenue > 0)
        .sort((a, b) => b.revenue - a.revenue)
        .slice(0, limit);
    },
    unpaidInvoices: (_: unknown, args: { status?: InvoiceStatus }) => {
      const wanted: InvoiceStatus[] = args.status ? [args.status] : ["UNPAID", "OVERDUE"];
      return invoices.filter((i) => wanted.includes(i.status));
    },
    order: (_: unknown, args: { id: string }) => orderById(args.id) ?? null,
  },
  Customer: {
    orders: (c: (typeof customers)[number], args: { status?: string }) =>
      ordersOf(c.id)
        .filter((o) => !args.status || o.status === args.status)
        .sort((a, b) => b.createdAt.localeCompare(a.createdAt)),
    invoices: (c: (typeof customers)[number], args: { status?: InvoiceStatus }) =>
      invoicesOfCustomer(c.id).filter((i) => !args.status || i.status === args.status),
    totalRevenue: (c: (typeof customers)[number]) =>
      ordersOf(c.id)
        .filter((o) => o.status !== "CANCELLED")
        .reduce((sum, o) => sum + o.amount, 0),
    outstandingBalance: (c: (typeof customers)[number]) =>
      invoicesOfCustomer(c.id)
        .filter((i) => i.status === "UNPAID" || i.status === "OVERDUE")
        .reduce((sum, i) => sum + i.amount, 0),
  },
  Order: {
    customer: (o: (typeof orders)[number]) => customerById(o.customerId),
    invoices: (o: (typeof orders)[number]) => invoicesOfOrder(o.id),
  },
  Invoice: {
    order: (i: (typeof invoices)[number]) => orderById(i.orderId),
  },
};

const server = new ApolloServer({
  typeDefs,
  resolvers,
  // Guardrail: cap query depth so an agent cannot submit a runaway nested query.
  validationRules: [depthLimit(MAX_DEPTH)],
  introspection: true, // fine for this internal demo; restrict it in public production APIs
});

const { url } = await startStandaloneServer(server, { listen: { port: 4000 } });
console.log(`GraphQL API ready at ${url} (max query depth ${MAX_DEPTH})`);
