using System.Net.Http.Json;
using bento_blazor.Models;

namespace bento_blazor.Services;

public class BentoApiService
{
    private readonly HttpClient _httpClient;

    public BentoApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<MenuItemDto>> GetMenuAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<MenuItemDto>>("/api/menus", cancellationToken) ?? new List<MenuItemDto>();
    }

    public async Task<List<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<UserDto>>("/api/users", cancellationToken) ?? new List<UserDto>();
    }

    public async Task<List<OrderDto>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<OrderDto>>("/api/orders", cancellationToken) ?? new List<OrderDto>();
    }

    public async Task CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/orders", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateOrderStatusAsync(int orderId, string status, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PatchAsJsonAsync($"/api/orders/{orderId}/status", new { status }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
