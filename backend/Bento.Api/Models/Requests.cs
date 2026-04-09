using System.ComponentModel.DataAnnotations;

namespace Bento.Api.Models;

public class CreateUserRequest
{
    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(120)]
    public string Email { get; set; } = string.Empty;
}

public class CreateMenuItemRequest
{
    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 9999)]
    public decimal Price { get; set; }

    public bool IsAvailable { get; set; } = true;
}

public class UpdateMenuItemRequest
{
    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 9999)]
    public decimal Price { get; set; }

    public bool IsAvailable { get; set; } = true;
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

public class UpdateOrderStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}
