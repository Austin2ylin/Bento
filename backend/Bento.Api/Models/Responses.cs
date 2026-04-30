namespace Bento.Api.Models;

public record UserResponse(int Id, string Name, string Email, DateTime CreatedAt);

public record MenuItemResponse(int Id, string Name, decimal Price, bool IsAvailable, DateTime UpdatedAt);

public record OrderItemResponse(int MenuItemId, string MenuName, int Quantity, decimal UnitPrice);

public record OrderResponse(
    int Id,
    int UserId,
    string UserName,
    string Status,
    decimal TotalAmount,
    DateTime OrderedAt,
    IEnumerable<OrderItemResponse> Items);

public record OrderStatusResponse(int Id, string Status);
