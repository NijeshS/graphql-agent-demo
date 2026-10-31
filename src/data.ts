// Seed data for the demo: customers, their orders, and invoices.
// Dates sit in Q4 2026 so "this quarter" questions work during the session.

export interface Customer {
  id: string;
  name: string;
  email: string;
  country: string;
  signedUp: string; // ISO date
}

export interface Order {
  id: string;
  customerId: string;
  amount: number; // USD
  status: "FULFILLED" | "PENDING" | "CANCELLED";
  createdAt: string; // ISO date
}

export type InvoiceStatus = "PAID" | "UNPAID" | "OVERDUE";

export interface Invoice {
  id: string;
  orderId: string;
  amount: number; // USD
  status: InvoiceStatus;
  dueDate: string; // ISO date
}

export const customers: Customer[] = [
  { id: "c1", name: "Acme Retail Group", email: "ap@acmeretail.example", country: "US", signedUp: "2024-03-11" },
  { id: "c2", name: "Northwind Traders", email: "billing@northwind.example", country: "US", signedUp: "2023-08-02" },
  { id: "c3", name: "Globex GmbH", email: "finance@globex.example", country: "DE", signedUp: "2024-01-25" },
  { id: "c4", name: "Initech LLC", email: "accounts@initech.example", country: "US", signedUp: "2025-05-19" },
  { id: "c5", name: "Umbrella Health", email: "payables@umbrella.example", country: "UK", signedUp: "2023-11-30" },
  { id: "c6", name: "Stark Components", email: "ap@starkcomponents.example", country: "US", signedUp: "2025-02-14" },
  { id: "c7", name: "Wayne Logistics", email: "finance@waynelogistics.example", country: "CA", signedUp: "2024-07-08" },
  { id: "c8", name: "Soylent Foods", email: "billing@soylent.example", country: "US", signedUp: "2025-09-21" },
];

export const orders: Order[] = [
  { id: "o1001", customerId: "c1", amount: 12400, status: "FULFILLED", createdAt: "2026-10-03" },
  { id: "o1002", customerId: "c1", amount: 8300, status: "FULFILLED", createdAt: "2026-10-21" },
  { id: "o1003", customerId: "c1", amount: 19750, status: "FULFILLED", createdAt: "2026-11-14" },
  { id: "o1004", customerId: "c2", amount: 22100, status: "FULFILLED", createdAt: "2026-10-09" },
  { id: "o1005", customerId: "c2", amount: 6400, status: "PENDING", createdAt: "2026-11-28" },
  { id: "o1006", customerId: "c3", amount: 31200, status: "FULFILLED", createdAt: "2026-10-17" },
  { id: "o1007", customerId: "c3", amount: 9800, status: "FULFILLED", createdAt: "2026-11-05" },
  { id: "o1008", customerId: "c4", amount: 4300, status: "FULFILLED", createdAt: "2026-10-26" },
  { id: "o1009", customerId: "c5", amount: 27600, status: "FULFILLED", createdAt: "2026-11-11" },
  { id: "o1010", customerId: "c5", amount: 11200, status: "FULFILLED", createdAt: "2026-10-30" },
  { id: "o1011", customerId: "c6", amount: 15800, status: "FULFILLED", createdAt: "2026-11-19" },
  { id: "o1012", customerId: "c6", amount: 2900, status: "CANCELLED", createdAt: "2026-11-22" },
  { id: "o1013", customerId: "c7", amount: 9100, status: "FULFILLED", createdAt: "2026-10-13" },
  { id: "o1014", customerId: "c8", amount: 5600, status: "FULFILLED", createdAt: "2026-11-25" },
  { id: "o1015", customerId: "c2", amount: 17400, status: "FULFILLED", createdAt: "2026-08-15" }, // Q3 - outside "this quarter"
];

export const invoices: Invoice[] = [
  { id: "inv-501", orderId: "o1001", amount: 12400, status: "PAID", dueDate: "2026-11-02" },
  { id: "inv-502", orderId: "o1002", amount: 8300, status: "UNPAID", dueDate: "2026-11-20" },
  { id: "inv-503", orderId: "o1003", amount: 19750, status: "UNPAID", dueDate: "2026-12-14" },
  { id: "inv-504", orderId: "o1004", amount: 22100, status: "PAID", dueDate: "2026-11-08" },
  { id: "inv-505", orderId: "o1005", amount: 6400, status: "UNPAID", dueDate: "2026-12-28" },
  { id: "inv-506", orderId: "o1006", amount: 31200, status: "PAID", dueDate: "2026-11-16" },
  { id: "inv-507", orderId: "o1007", amount: 9800, status: "OVERDUE", dueDate: "2026-12-05" },
  { id: "inv-508", orderId: "o1008", amount: 4300, status: "PAID", dueDate: "2026-11-25" },
  { id: "inv-509", orderId: "o1009", amount: 27600, status: "UNPAID", dueDate: "2026-12-11" },
  { id: "inv-510", orderId: "o1010", amount: 11200, status: "OVERDUE", dueDate: "2026-11-29" },
  { id: "inv-511", orderId: "o1011", amount: 15800, status: "PAID", dueDate: "2026-12-19" },
  { id: "inv-512", orderId: "o1013", amount: 9100, status: "PAID", dueDate: "2026-11-12" },
  { id: "inv-513", orderId: "o1014", amount: 5600, status: "UNPAID", dueDate: "2026-12-25" },
  { id: "inv-514", orderId: "o1015", amount: 17400, status: "PAID", dueDate: "2026-09-14" },
];

export const customerById = (id: string) => customers.find((c) => c.id === id);
export const orderById = (id: string) => orders.find((o) => o.id === id);
export const ordersOf = (customerId: string) => orders.filter((o) => o.customerId === customerId);
export const invoicesOfOrder = (orderId: string) => invoices.filter((i) => i.orderId === orderId);
export const invoicesOfCustomer = (customerId: string) =>
  invoices.filter((i) => orderById(i.orderId)?.customerId === customerId);
