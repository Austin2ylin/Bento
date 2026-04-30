using Bento.Api.Constants;
using Bento.Api.Data;
using Bento.Api.Models;
using Bento.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bento.Api.Controllers;

[ApiController]
[Route("api/menus")]
public class MenuController : ControllerBase
{
    private readonly BentoDbContext _dbContext;
    private readonly IRedisService _redisService;

    public MenuController(BentoDbContext dbContext, IRedisService redisService)
    {
        _dbContext = dbContext;
        _redisService = redisService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MenuItemResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var cached = await _redisService.GetAsync<List<MenuItemResponse>>(CacheKeys.MenuList, cancellationToken);
        if (cached is not null)
        {
            return Ok(cached);
        }

        var items = await _dbContext.MenuItems
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new MenuItemResponse(x.Id, x.Name, x.Price, x.IsAvailable, x.UpdatedAt))
            .ToListAsync(cancellationToken);

        await _redisService.SetAsync(CacheKeys.MenuList, items, TimeSpan.FromMinutes(10), cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:int:min(1)}")]
    public async Task<ActionResult<MenuItemResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _dbContext.MenuItems
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new MenuItemResponse(x.Id, x.Name, x.Price, x.IsAvailable, x.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<MenuItemResponse>> Create([FromBody] CreateMenuItemRequest request, CancellationToken cancellationToken)
    {
        var menuItem = new MenuItem
        {
            Name = request.Name.Trim(),
            Price = request.Price,
            IsAvailable = request.IsAvailable,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.MenuItems.Add(menuItem);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _redisService.RemoveAsync(CacheKeys.MenuList, cancellationToken);

        var response = new MenuItemResponse(menuItem.Id, menuItem.Name, menuItem.Price, menuItem.IsAvailable, menuItem.UpdatedAt);
        return CreatedAtAction(nameof(GetById), new { id = menuItem.Id }, response);
    }

    [HttpPut("{id:int:min(1)}")]
    public async Task<ActionResult<MenuItemResponse>> Update(int id, [FromBody] UpdateMenuItemRequest request, CancellationToken cancellationToken)
    {
        var menuItem = await _dbContext.MenuItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (menuItem is null)
        {
            return NotFound();
        }

        menuItem.Name = request.Name.Trim();
        menuItem.Price = request.Price;
        menuItem.IsAvailable = request.IsAvailable;
        menuItem.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _redisService.RemoveAsync(CacheKeys.MenuList, cancellationToken);

        return Ok(new MenuItemResponse(menuItem.Id, menuItem.Name, menuItem.Price, menuItem.IsAvailable, menuItem.UpdatedAt));
    }

    [HttpDelete("{id:int:min(1)}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var menuItem = await _dbContext.MenuItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (menuItem is null)
        {
            return NotFound();
        }

        _dbContext.MenuItems.Remove(menuItem);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _redisService.RemoveAsync(CacheKeys.MenuList, cancellationToken);

        return NoContent();
    }
}
