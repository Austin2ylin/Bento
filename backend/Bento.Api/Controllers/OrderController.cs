using Bento.Api.Data;
using Bento.Api.Models;
using Bento.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bento.Api.Controllers;

[ApiController]
[Route("api/order")]
public class OrderController : ControllerBase
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.Ordinal)
    {
        "待確認",
        "製作中",
        "已完成",
        "已取消"
    };

    private readonly BentoDbContext _dbContext;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly IMongoService _mongoService;
    private readonly ILogger<OrderController> _logger;

    public OrderController(
        BentoDbContext dbContext,
        IRabbitMqService rabbitMqService,
        IMongoService mongoService,
        ILogger<OrderController> logger)
    {
        _dbContext = dbContext;
        _rabbitMqService = rabbitMqService;
        _mongoService = mongoService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var orders = await _dbContext.Orders
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Items)
            .ThenInclude(x => x.MenuItem)
            .OrderByDescending(x => x.OrderedAt)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                UserName = x.User != null ? x.User.Name : string.Empty,
                x.Status,
                x.TotalAmount,
                x.OrderedAt,
                Items = x.Items.Select(i => new
                {
                    i.MenuItemId,
                    MenuName = i.MenuItem != null ? i.MenuItem.Name : string.Empty,
                    i.Quantity,
                    i.UnitPrice
                })
            })
            .ToListAsync(cancellationToken);

        return Ok(orders);
    }

    [HttpGet("{id:int:min(1)}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Items)
            .ThenInclude(x => x.MenuItem)
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                UserName = x.User != null ? x.User.Name : string.Empty,
                x.Status,
                x.TotalAmount,
                x.OrderedAt,
                Items = x.Items.Select(i => new
                {
                    i.MenuItemId,
                    MenuName = i.MenuItem != null ? i.MenuItem.Name : string.Empty,
                    i.Quantity,
                    i.UnitPrice
                })
            })
            .FirstOrDefaultAsync(cancellationToken);

        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var userExists = await _dbContext.Users.AnyAsync(x => x.Id == request.UserId, cancellationToken);
        if (!userExists)
        {
            return BadRequest(new { message = "找不到指定使用者。" });
        }

        var normalizedItems = request.Items
            .GroupBy(x => x.MenuItemId)
            .Select(group => new
            {
                MenuItemId = group.Key,
                Quantity = group.Sum(x => x.Quantity)
            })
            .ToList();

        var menuIds = normalizedItems.Select(x => x.MenuItemId).ToList();
        var menuMap = await _dbContext.MenuItems
            .Where(x => menuIds.Contains(x.Id) && x.IsAvailable)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (menuMap.Count != menuIds.Count)
        {
            return BadRequest(new { message = "部分菜單不存在或已停售。" });
        }

        var order = new Order
        {
            UserId = request.UserId,
            Status = "待確認",
            OrderedAt = DateTime.UtcNow
        };

        foreach (var item in normalizedItems)
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

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await _rabbitMqService.PublishOrderCreatedAsync(order, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RabbitMQ 推送失敗，OrderId: {OrderId}", order.Id);
        }

        try
        {
            await _mongoService.LogOrderAsync(order, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MongoDB 訂單紀錄寫入失敗，OrderId: {OrderId}", order.Id);
        }

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, new
        {
            order.Id,
            order.UserId,
            order.Status,
            order.TotalAmount,
            order.OrderedAt,
            Items = order.Items.Select(i => new
            {
                i.MenuItemId,
                i.Quantity,
                i.UnitPrice
            })
        });
    }

    [HttpPatch("{id:int:min(1)}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var status = request.Status.Trim();
        if (!AllowedStatuses.Contains(status))
        {
            return BadRequest(new { message = "狀態不合法，僅允許：待確認、製作中、已完成、已取消。" });
        }

        var order = await _dbContext.Orders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        order.Status = status;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            order.Id,
            order.Status
        });
    }
}
