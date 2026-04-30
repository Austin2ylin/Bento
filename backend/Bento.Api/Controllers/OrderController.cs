using Bento.Api.Constants;
using Bento.Api.Data;
using Bento.Api.Models;
using Bento.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bento.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrderController : ControllerBase
{
    private readonly BentoDbContext _dbContext;
    private readonly IOrderService _orderService;

    public OrderController(
        BentoDbContext dbContext,
        IOrderService orderService)
    {
        _dbContext = dbContext;
        _orderService = orderService;
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
            .Select(x => new OrderResponse(
                x.Id,
                x.UserId,
                x.User != null ? x.User.Name : string.Empty,
                x.Status,
                x.TotalAmount,
                x.OrderedAt,
                x.Items.Select(i => new OrderItemResponse(
                    i.MenuItemId,
                    i.MenuItem != null ? i.MenuItem.Name : string.Empty,
                    i.Quantity,
                    i.UnitPrice))))
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
            .Select(x => new OrderResponse(
                x.Id,
                x.UserId,
                x.User != null ? x.User.Name : string.Empty,
                x.Status,
                x.TotalAmount,
                x.OrderedAt,
                x.Items.Select(i => new OrderItemResponse(
                    i.MenuItemId,
                    i.MenuItem != null ? i.MenuItem.Name : string.Empty,
                    i.Quantity,
                    i.UnitPrice))))
            .FirstOrDefaultAsync(cancellationToken);

        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await _orderService.CreateAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            return result.Error switch
            {
                OrderServiceError.UserNotFound => BadRequest(new { message = "找不到指定使用者。" }),
                OrderServiceError.MenuUnavailable => BadRequest(new { message = "部分菜單不存在或已停售。" }),
                _ => BadRequest()
            };
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPatch("{id:int:min(1)}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _orderService.UpdateStatusAsync(id, request, cancellationToken);
        if (!result.Succeeded)
        {
            return result.Error switch
            {
                OrderServiceError.InvalidStatus => BadRequest(new { message = $"狀態不合法，僅允許：{string.Join('、', OrderStatuses.All)}。" }),
                OrderServiceError.OrderNotFound => NotFound(),
                _ => BadRequest()
            };
        }

        return Ok(result.Value);
    }
}
