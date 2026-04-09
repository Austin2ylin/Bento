using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Bento.Api.Models;

public class MenuItem
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [Precision(18, 2)]
    public decimal Price { get; set; }

    public bool IsAvailable { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // 一個菜單品項可出現在多筆訂單，透過中介表 OrderItem 建立多對多
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
