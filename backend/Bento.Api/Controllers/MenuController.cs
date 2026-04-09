using Bento.Api.Data;
using Bento.Api.Models;
using Bento.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bento.Api.Controllers;

[ApiController]
[Route("api/menu")]
public class MenuController : ControllerBase
{
    private const string MenuCacheKey = "menu:list";
    private readonly BentoDbContext _dbContext;
    private readonly IRedisService _redisService;

    public MenuController(BentoDbContext dbContext, IRedisService redisService)
    {
        _dbContext = dbContext;
        _redisService = redisService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MenuItem>>> GetAll(CancellationToken cancellationToken)
    {
        var menu = await _dbContext.MenuItems
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return Ok(menu);
    }

    [HttpPost]
    public async Task<ActionResult<MenuItem>> Create([FromBody] CreateMenuItemRequest request, CancellationToken cancellationToken)
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

        await _redisService.RemoveAsync(MenuCacheKey, cancellationToken);

        return Created($"/api/menu/{menuItem.Id}", menuItem);
    }

    [HttpPut("{id:int:min(1)}")]
    public async Task<ActionResult<MenuItem>> Update(int id, [FromBody] UpdateMenuItemRequest request, CancellationToken cancellationToken)
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
        await _redisService.RemoveAsync(MenuCacheKey, cancellationToken);

        return Ok(menuItem);
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

        await _redisService.RemoveAsync(MenuCacheKey, cancellationToken);

        return NoContent();
    }
}
