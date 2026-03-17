using Dapper;
using MediatR;
using SharedKernel.Common;
using OrderService.Application.Interfaces;

namespace OrderService.Application.Queries;

// 1. DTOs
public record OrderItemDto(Guid Id, string ProductId, int Quantity, decimal Price);
public record OrderDto(Guid Id, Guid UserId, string Status, decimal TotalAmount, string Currency, DateTime CreatedAt, List<OrderItemDto> Items);

// 2. Query Object
public record GetOrderWithItemsQuery(Guid OrderId) : IRequest<Result<OrderDto>>;

// 3. Query Handler (Dapper Multi-Mapping)
public sealed class GetOrderWithItemsQueryHandler : IRequestHandler<GetOrderWithItemsQuery, Result<OrderDto>>
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    public GetOrderWithItemsQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
    }

    public async Task<Result<OrderDto>> Handle(GetOrderWithItemsQuery request, CancellationToken cancellationToken)
    {
        using var connection = _sqlConnectionFactory.CreateConnection();

        // Cú pháp JOIN truyền thống của SQL để lấy dữ liệu từ 2 bảng: Orders và OrderItems
        const string sql = @"
            SELECT 
                o.""Id"", o.""UserId"", CAST(o.""Status"" AS integer) AS ""Status"", 
                o.""TotalAmount"", o.""Currency"", o.""CreatedAt"",
                oi.""Id"", oi.""ProductId"", oi.""Quantity"", oi.""Price""
            FROM ""Orders"" o
            LEFT JOIN ""OrderItems"" oi ON o.""Id"" = oi.""OrderId""
            WHERE o.""Id"" = @OrderId;
        ";

        // Dictionary dùng để gom (Group) các OrderItems vào chung 1 Order
        var orderDictionary = new Dictionary<Guid, OrderDto>();

        // Gọi Dapper QueryAsync với Multi-Mapping (Map 2 Objects trên cùng 1 row)
        var result = await connection.QueryAsync<OrderDto, OrderItemDto, OrderDto>(
            sql,
            map: (order, orderItem) =>
            {
                // Nếu Order chưa có trong Dictionary thì nhét vào và khởi tạo danh sách Items
                if (!orderDictionary.TryGetValue(order.Id, out var currentOrder))
                {
                    currentOrder = order with { Items = new List<OrderItemDto>() };
                    orderDictionary.Add(currentOrder.Id, currentOrder);
                }

                // Nếu có OrderItem đi kèm trên Row này (LEFT JOIN) thì tống nó vào List Items
                if (orderItem != null && currentOrder.Items != null)
                {
                    currentOrder.Items.Add(orderItem);
                }

                return currentOrder;
            },
            param: new { OrderId = request.OrderId },
            splitOn: "Id" // Cột làm mốc để Dapper biết đâu là điểm cắt tách biệt giữa Order và OrderItem
        );

        // Lấy ra Order duy nhất từ Dictionary
        var finalOrder = orderDictionary.Values.FirstOrDefault();

        if (finalOrder is null)
        {
            return Result<OrderDto>.Failure($"Order with ID {request.OrderId} was not found.");
        }

        return Result<OrderDto>.Success(finalOrder);
    }
}
