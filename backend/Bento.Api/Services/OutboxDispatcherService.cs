using Bento.Api.Constants;
using Bento.Api.Data;
using Bento.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Bento.Api.Services;

public class OutboxDispatcherService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 20;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcherService> _logger;

    public OutboxDispatcherService(IServiceScopeFactory scopeFactory, ILogger<OutboxDispatcherService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        do
        {
            await DispatchDueMessagesAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task DispatchDueMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BentoDbContext>();
            var rabbitMqService = scope.ServiceProvider.GetRequiredService<IRabbitMqService>();
            var mongoService = scope.ServiceProvider.GetRequiredService<IMongoService>();

            var now = DateTime.UtcNow;
            var messages = await dbContext.OutboxMessages
                .Where(x => x.ProcessedAt == null && x.NextAttemptAt <= now)
                .OrderBy(x => x.CreatedAt)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            foreach (var message in messages)
            {
                await DispatchMessageAsync(dbContext, rabbitMqService, mongoService, message, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Outbox 背景派送發生未預期錯誤。");
        }
    }

    private async Task DispatchMessageAsync(
        BentoDbContext dbContext,
        IRabbitMqService rabbitMqService,
        IMongoService mongoService,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = await dbContext.Orders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == message.AggregateId, cancellationToken);

            if (order is null)
            {
                MarkProcessed(message, $"找不到訂單 {message.AggregateId}，略過 outbox 訊息。");
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            switch (message.Type)
            {
                case OutboxMessageTypes.OrderCreatedRabbitMq:
                    await rabbitMqService.PublishOrderCreatedAsync(order, cancellationToken);
                    break;
                case OutboxMessageTypes.OrderCreatedMongoLog:
                    await mongoService.LogOrderAsync(order, cancellationToken);
                    break;
                default:
                    MarkProcessed(message, $"未知的 outbox 訊息類型：{message.Type}");
                    await dbContext.SaveChangesAsync(cancellationToken);
                    return;
            }

            MarkProcessed(message);
        }
        catch (Exception ex)
        {
            message.AttemptCount++;
            message.LastError = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
            message.NextAttemptAt = DateTime.UtcNow.AddSeconds(Math.Min(300, Math.Pow(2, message.AttemptCount)));

            _logger.LogWarning(
                ex,
                "Outbox 訊息派送失敗，MessageId: {MessageId}, Type: {Type}, Attempt: {AttemptCount}",
                message.Id,
                message.Type,
                message.AttemptCount);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void MarkProcessed(OutboxMessage message, string? note = null)
    {
        message.ProcessedAt = DateTime.UtcNow;
        message.LastError = note;
    }
}
