using System.Security.Cryptography;
using System.Text;
using Bento.Api.Constants;
using Bento.Api.Data;
using Bento.Api.Models;
using Bento.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bento.Api.Controllers;

[ApiController]
[Route("api/cache")]
public class CacheController : ControllerBase
{
    private readonly BentoDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IRedisService _redisService;

    public CacheController(
        BentoDbContext dbContext,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IRedisService redisService)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _environment = environment;
        _redisService = redisService;
    }

    [HttpGet("menu")]
    public async Task<IActionResult> GetMenuCache(CancellationToken cancellationToken)
    {
        var cached = await _redisService.GetAsync<List<MenuItemResponse>>(CacheKeys.MenuList, cancellationToken);
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
            .OrderBy(x => x.Id)
            .Select(x => new MenuItemResponse(x.Id, x.Name, x.Price, x.IsAvailable, x.UpdatedAt))
            .ToListAsync(cancellationToken);

        await _redisService.SetAsync(CacheKeys.MenuList, menu, TimeSpan.FromMinutes(10), cancellationToken);

        return Ok(new
        {
            source = "database",
            data = menu
        });
    }

    [HttpDelete("menu")]
    public async Task<IActionResult> ClearMenuCache(CancellationToken cancellationToken)
    {
        if (!IsCacheAdminAuthorized())
        {
            return Unauthorized(new { message = "缺少或無效的快取管理金鑰。" });
        }

        await _redisService.RemoveAsync(CacheKeys.MenuList, cancellationToken);
        return NoContent();
    }

    private bool IsCacheAdminAuthorized()
    {
        var expectedKey = _configuration["Cache:AdminApiKey"];
        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            return !_environment.IsProduction();
        }

        if (!Request.Headers.TryGetValue("X-Cache-Admin-Key", out var providedKey))
        {
            return false;
        }

        var expectedBytes = Encoding.UTF8.GetBytes(expectedKey);
        var providedBytes = Encoding.UTF8.GetBytes(providedKey.ToString());

        return expectedBytes.Length == providedBytes.Length
            && CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
