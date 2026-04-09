using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Bento.Api.Models;

public class Order
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public DateTime OrderedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(20)]
    public string Status { get; set; } = "待確認";

    [Precision(18, 2)]
    public decimal TotalAmount { get; set; }

    // 一張訂單包含多個品項（一對多到 OrderItem）
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
