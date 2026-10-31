// The same data as a plain REST API - used in the session for contrast.
// Answering "top 5 customers by revenue this quarter, with their unpaid
// invoices" takes several calls here (list orders -> group client-side ->
// fetch each customer -> fetch each customer's invoices), while the GraphQL
// API answers it with one query (see README).
//
// Run:  dotnet run --project src/RestApi     ->  http://localhost:4100

using System.Text.Json;
using System.Text.Json.Serialization;
using Shared;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:4100");
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper));
});
var app = builder.Build();

app.MapGet("/customers", () => Store.Customers);

app.MapGet("/customers/{id}", (string id) =>
    Store.CustomerById(id) is { } c ? Results.Ok(c) : Results.NotFound(new { error = "customer not found" }));

app.MapGet("/customers/{id}/orders", (string id) => Store.OrdersOf(id));

app.MapGet("/customers/{id}/invoices", (string id) => Store.InvoicesOfCustomer(id));

app.MapGet("/orders", () => Store.Orders);

app.MapGet("/invoices", (string? status) =>
    status is null
        ? Store.Invoices
        : Store.Invoices.Where(i => i.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase)));

app.Run();
