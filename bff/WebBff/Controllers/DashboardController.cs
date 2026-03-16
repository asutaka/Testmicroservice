using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace WebBff.Controllers;

/// <summary>
/// Web BFF: Aggregates data from multiple services into a single response optimized for web clients.
/// Sits between the API Gateway and downstream services.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
public sealed class DashboardController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IHttpClientFactory httpClientFactory, ILogger<DashboardController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Aggregated dashboard: user profile + recent orders in a single call.
    /// BFF pattern: fan-out to UserService and OrderService simultaneously.
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType(typeof(UserDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserDashboard(Guid userId, CancellationToken cancellationToken)
    {
        var userClient = _httpClientFactory.CreateClient("UserService");
        var orderClient = _httpClientFactory.CreateClient("OrderService");

        // Fan-out: call both services in parallel (BFF pattern)
        var userTask = userClient.GetFromJsonAsync<UserSummary>($"/api/v1/users/{userId}", cancellationToken);
        var ordersTask = orderClient.GetFromJsonAsync<IEnumerable<OrderSummary>>($"/api/v1/orders?userId={userId}", cancellationToken);

        await Task.WhenAll(userTask, ordersTask);

        var user = await userTask;
        var orders = await ordersTask ?? [];

        if (user is null)
            return NotFound($"User '{userId}' not found.");

        _logger.LogInformation("Dashboard fetched for User {UserId}: {OrderCount} orders", userId, orders.Count());

        return Ok(new UserDashboardDto(
            user,
            orders.ToList(),
            orders.Sum(o => o.TotalAmount),
            orders.Count()
        ));
    }
}

// ── DTOs (web-optimized, denormalized) ────────────────────────────────────────

public record UserDashboardDto(
    UserSummary User,
    List<OrderSummary> RecentOrders,
    decimal TotalSpent,
    int TotalOrders
);

public record UserSummary(Guid Id, string FullName, string Email, bool IsActive);
public record OrderSummary(Guid Id, string Status, decimal TotalAmount, string Currency, DateTime CreatedAt);
