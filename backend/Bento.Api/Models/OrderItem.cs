using Microsoft.EntityFrameworkCore;

namespace Bento.Api.Models;

public class OrderItem
{
    public int OrderId { get; set; }

    public Order? Order { get; set; }

    public int MenuItemId { get; set; }

    public MenuItem? MenuItem { get; set; }

    public int Quantity { get; set; }

    [Precision(18, 2)]
    public decimal UnitPrice { get; set; }

    // Order 與 MenuItem 透過此中介表形成多對多
}
