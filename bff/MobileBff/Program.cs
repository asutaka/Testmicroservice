using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Mobile BFF",
        Version = "v1",
        Description = "BFF optimized for mobile clients — returns minimal, compressed payloads"
    });
});

builder.Services.AddHttpClient("UserService", client =>
    client.BaseAddress = new Uri(builder.Configuration["Services:UserService"] ?? "http://localhost:5001"));

builder.Services.AddHttpClient("OrderService", client =>
    client.BaseAddress = new Uri(builder.Configuration["Services:OrderService"] ?? "http://localhost:5002"));

builder.Services.AddResponseCompression();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseResponseCompression();
app.MapControllers();
app.MapHealthChecks("/health");

// ── Minimal mobile endpoints (inline for brevity) ─────────────────────────────

// Mobile: light user profile
app.MapGet("/mobile/user/{id:guid}", async (Guid id, IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("UserService");
    var response = await client.GetAsync($"/api/v1/users/{id}");
    return response.IsSuccessStatusCode
        ? Results.Ok(await response.Content.ReadFromJsonAsync<object>())
        : Results.NotFound();
});

// Mobile: last 5 orders only
app.MapGet("/mobile/user/{id:guid}/orders", async (Guid id, IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("OrderService");
    var response = await client.GetAsync($"/api/v1/orders?userId={id}&limit=5");
    return response.IsSuccessStatusCode
        ? Results.Ok(await response.Content.ReadFromJsonAsync<object>())
        : Results.Problem("Order service unavailable");
});

app.Run();
