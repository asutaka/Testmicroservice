using EventBus.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Contracts.Events;
using OrderService.Domain.Entities;
using SharedKernel.Common;
using UserService.Contracts.Grpc;

namespace OrderService.Application.Commands.CreateOrder;

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IEventBus _eventBus;
    private readonly UserGrpc.UserGrpcClient _userGrpcClient;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IEventBus eventBus,
        UserGrpc.UserGrpcClient userGrpcClient,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _eventBus = eventBus;
        _userGrpcClient = userGrpcClient;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // ── Step 1: Validate user exists via gRPC ──────────────────────────
            var userResponse = await _userGrpcClient.UserExistsAsync(
                new UserExistsRequest { UserId = request.UserId.ToString() },
                cancellationToken: cancellationToken);

            if (!userResponse.Exists)
                return Result<OrderDto>.Failure($"User '{request.UserId}' not found or inactive.");

            // ── Step 2: Create Order aggregate ─────────────────────────────────
            var order = Order.Create(request.UserId, request.Notes);

            foreach (var item in request.Items)
                order.AddItem(item.ProductId, item.ProductName, item.Quantity, item.UnitPrice);

            order.Confirm();

            // ── Step 3: Persist ────────────────────────────────────────────────
            await _orderRepository.AddAsync(order, cancellationToken);
            await _orderRepository.SaveChangesAsync(cancellationToken);

            // ── Step 4: Publish OrderCreated to RabbitMQ (async) ──────────────
            await _eventBus.PublishAsync(new OrderCreatedIntegrationEvent(
                order.Id,
                order.UserId,
                order.TotalAmount.Amount,
                order.TotalAmount.Currency,
                order.Items.Select(i => new OrderItemEvent(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice))
            ), cancellationToken);

            _logger.LogInformation("Order {OrderId} created for User {UserId}", order.Id, order.UserId);

            return Result<OrderDto>.Success(MapToDto(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for user {UserId}", request.UserId);
            return Result<OrderDto>.Failure($"Failed to create order: {ex.Message}");
        }
    }

    private static OrderDto MapToDto(Order order) => new(
        order.Id,
        order.UserId,
        order.Status,
        order.Status.ToString(),
        order.TotalAmount.Amount,
        order.TotalAmount.Currency,
        order.Items.Select(i => new OrderItemDto(i.Id, i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.TotalPrice)),
        order.Notes,
        order.CreatedAt,
        order.UpdatedAt);
}
