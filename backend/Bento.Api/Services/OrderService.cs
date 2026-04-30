using Bento.Api.Constants;
using Bento.Api.Data;
using Bento.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Bento.Api.Services;

public enum OrderServiceError
{
    None,
    UserNotFound,
    MenuUnavailable,
    OrderNotFound,
    InvalidStatus
}

public record OrderServiceResult<T>(T? Value, OrderServiceError Error = OrderServiceError.None)
{
    public bool Succeeded => Error == OrderServiceError.None;
}

public interface IOrderService
{
    Task<OrderServiceResult<OrderResponse>> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderServiceResult<OrderStatusResponse>> UpdateStatusAsync(int id, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default);
}

public class OrderService : IOrderService
{
    private readonly BentoDbContext _dbContext;

    public OrderService(BentoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OrderServiceResult<OrderResponse>> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == request.UserId)
            .Select(x => new { x.Id, x.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return new OrderServiceResult<OrderResponse>(null, OrderServiceError.UserNotFound);
        }

        var menuIds = request.Items.Select(x => x.MenuItemId).ToList();
        var menuMap = await _dbContext.MenuItems
            .Where(x => menuIds.Contains(x.Id) && x.IsAvailable)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (menuMap.Count != menuIds.Count)
        {
            return new OrderServiceResult<OrderResponse>(null, OrderServiceError.MenuUnavailable);
        }

        var order = new Order
        {
            UserId = request.UserId,
            Status = OrderStatuses.Pending,
            OrderedAt = DateTime.UtcNow
        };

        foreach (var item in request.Items)
        {
            var menuItem = menuMap[item.MenuItemId];
            order.Items.Add(new OrderItem
            {
                MenuItemId = menuItem.Id,
                Quantity = item.Quantity,
                UnitPrice = menuItem.Price
            });
        }

        order.TotalAmount = order.Items.Sum(x => x.UnitPrice * x.Quantity);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.OutboxMessages.AddRange(
            CreateOutboxMessage(OutboxMessageTypes.OrderCreatedRabbitMq, order.Id),
            CreateOutboxMessage(OutboxMessageTypes.OrderCreatedMongoLog, order.Id));

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var response = new OrderResponse(
            order.Id,
            order.UserId,
            user.Name,
            order.Status,
            order.TotalAmount,
            order.OrderedAt,
            order.Items.Select(i =>
            {
                var menuItem = menuMap[i.MenuItemId];
                return new OrderItemResponse(i.MenuItemId, menuItem.Name, i.Quantity, i.UnitPrice);
            }).ToList());

        return new OrderServiceResult<OrderResponse>(response);
    }

    public async Task<OrderServiceResult<OrderStatusResponse>> UpdateStatusAsync(
        int id,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = request.Status.Trim();
        if (!OrderStatuses.IsAllowed(status))
        {
            return new OrderServiceResult<OrderStatusResponse>(null, OrderServiceError.InvalidStatus);
        }

        var order = await _dbContext.Orders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (order is null)
        {
            return new OrderServiceResult<OrderStatusResponse>(null, OrderServiceError.OrderNotFound);
        }

        order.Status = status;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new OrderServiceResult<OrderStatusResponse>(new OrderStatusResponse(order.Id, order.Status));
    }

    private static OutboxMessage CreateOutboxMessage(string type, int orderId)
    {
        var now = DateTime.UtcNow;
        return new OutboxMessage
        {
            Type = type,
            AggregateId = orderId,
            CreatedAt = now,
            NextAttemptAt = now
        };
    }
}
