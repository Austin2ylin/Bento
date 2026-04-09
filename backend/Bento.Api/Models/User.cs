using System.ComponentModel.DataAnnotations;

namespace Bento.Api.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(120)]
    public string Email { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 一個使用者可以建立多筆訂單（一對多）
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
