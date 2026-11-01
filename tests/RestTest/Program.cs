// Smoke test for the REST API. Requires the API to be running
// (dotnet run --project src/RestApi).
// Run from the repo root: dotnet run --project tests/RestTest

using System.Text.Json;

using var http = new HttpClient { BaseAddress = new Uri("http://localhost:4100") };

var customersResponse = await http.GetAsync("/customers");
customersResponse.EnsureSuccessStatusCode();
using var customersJson = JsonDocument.Parse(await customersResponse.Content.ReadAsStringAsync());
var customers = customersJson.RootElement;
Assert(customers.GetArrayLength() == 8, "Expected 8 customers.");
Assert(
    customers.EnumerateArray().Any(customer => customer.GetProperty("name").GetString() == "Acme Retail Group"),
    "Expected the customer list to include Acme Retail Group.");
Console.WriteLine("GET /customers: received 8 customers.");

var customerResponse = await http.GetAsync("/customers/c1");
customerResponse.EnsureSuccessStatusCode();
using var customerJson = JsonDocument.Parse(await customerResponse.Content.ReadAsStringAsync());
Assert(
    customerJson.RootElement.GetProperty("name").GetString() == "Acme Retail Group",
    "Expected customer c1 to be Acme Retail Group.");
Console.WriteLine("GET /customers/c1: received Acme Retail Group.");

var ordersResponse = await http.GetAsync("/customers/c1/orders");
ordersResponse.EnsureSuccessStatusCode();
using var ordersJson = JsonDocument.Parse(await ordersResponse.Content.ReadAsStringAsync());
Assert(ordersJson.RootElement.GetArrayLength() == 3, "Expected 3 orders for customer c1.");
Console.WriteLine("GET /customers/c1/orders: received 3 orders.");

var invoicesResponse = await http.GetAsync("/customers/c1/invoices");
invoicesResponse.EnsureSuccessStatusCode();
using var invoicesJson = JsonDocument.Parse(await invoicesResponse.Content.ReadAsStringAsync());
Assert(invoicesJson.RootElement.GetArrayLength() == 3, "Expected 3 invoices for customer c1.");
Console.WriteLine("GET /customers/c1/invoices: received 3 invoices.");

var overdueResponse = await http.GetAsync("/invoices?status=OVERDUE");
overdueResponse.EnsureSuccessStatusCode();
using var overdueJson = JsonDocument.Parse(await overdueResponse.Content.ReadAsStringAsync());
Assert(overdueJson.RootElement.GetArrayLength() == 2, "Expected 2 overdue invoices.");
Console.WriteLine("GET /invoices?status=OVERDUE: received 2 invoices.");

var missingCustomerResponse = await http.GetAsync("/customers/unknown");
Assert(missingCustomerResponse.StatusCode == System.Net.HttpStatusCode.NotFound,
    "Expected an unknown customer to return 404.");
Console.WriteLine("GET /customers/unknown: received 404 as expected.");

Console.WriteLine("\nREST API smoke test passed.");

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
