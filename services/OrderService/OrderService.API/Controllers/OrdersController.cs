using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Commands.CreateOrder;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;

namespace OrderService.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IOrderRepository _orderRepository;

    public OrdersController(IMediator mediator, IOrderRepository orderRepository)
    {
        _mediator = mediator;
        _orderRepository = orderRepository;
    }

    /// <summary>Get all orders</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetAllAsync(cancellationToken);
        var dtos = orders.Select(o => new OrderDto(
            o.Id, o.UserId, o.Status, o.Status.ToString(),
            o.TotalAmount.Amount, o.TotalAmount.Currency,
            o.Items.Select(i => new OrderItemDto(i.Id, i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.TotalPrice)),
            o.Notes, o.CreatedAt, o.UpdatedAt));
        return Ok(dtos);
    }

    /// <summary>Get order by ID</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(id, cancellationToken);
        if (order is null) return NotFound($"Order '{id}' not found.");

        return Ok(new OrderDto(
            order.Id, order.UserId, order.Status, order.Status.ToString(),
            order.TotalAmount.Amount, order.TotalAmount.Currency,
            order.Items.Select(i => new OrderItemDto(i.Id, i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.TotalPrice)),
            order.Notes, order.CreatedAt, order.UpdatedAt));
    }

    /// <summary>Create a new order (calls UserService via gRPC, publishes OrderCreated to RabbitMQ)</summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateOrderCommand(
            request.UserId,
            request.Items.Select(i => new CreateOrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)),
            request.Notes);

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result.ErrorMessage);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }
}

public sealed record CreateOrderRequest(
    Guid UserId,
    IEnumerable<CreateOrderItemRequest> Items,
    string? Notes = null
);

public sealed record CreateOrderItemRequest(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);
