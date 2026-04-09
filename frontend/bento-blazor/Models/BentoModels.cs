namespace bento_blazor.Models;

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class MenuItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
}

public class OrderItemDto
{
    public int MenuItemId { get; set; }
    public string? MenuName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string Status { get; set; } = "待確認";
    public decimal TotalAmount { get; set; }
    public DateTime OrderedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class CreateOrderRequest
{
    public int UserId { get; set; }
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public class CreateOrderItemRequest
{
    public int MenuItemId { get; set; }
    public int Quantity { get; set; }
}
