using Bento.Api.Data;
using Bento.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bento.Api.Controllers;

[ApiController]
[Route("api/cache")]
public class CacheController : ControllerBase
{
    private const string MenuCacheKey = "menu:list";
    private readonly BentoDbContext _dbContext;
    private readonly IRedisService _redisService;

    public CacheController(BentoDbContext dbContext, IRedisService redisService)
    {
        _dbContext = dbContext;
        _redisService = redisService;
    }

    [HttpGet("menu")]
    public async Task<IActionResult> GetMenuCache(CancellationToken cancellationToken)
    {
        var cached = await _redisService.GetAsync<List<MenuCacheItem>>(MenuCacheKey, cancellationToken);
        if (cached is not null)
        {
            return Ok(new
            {
                source = "redis",
                data = cached
            });
        }

        var menu = await _dbContext.MenuItems
            .AsNoTracking()
            .Where(x => x.IsAvailable)
            .OrderBy(x => x.Id)
            .Select(x => new MenuCacheItem
            {
                Id = x.Id,
                Name = x.Name,
                Price = x.Price,
                IsAvailable = x.IsAvailable
            })
            .ToListAsync(cancellationToken);

        await _redisService.SetAsync(MenuCacheKey, menu, TimeSpan.FromMinutes(10), cancellationToken);

        return Ok(new
        {
            source = "database",
            data = menu
        });
    }

    [HttpDelete("menu")]
    public async Task<IActionResult> ClearMenuCache(CancellationToken cancellationToken)
    {
        await _redisService.RemoveAsync(MenuCacheKey, cancellationToken);
        return NoContent();
    }

    public class MenuCacheItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; }
    }
}
