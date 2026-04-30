using System.Text;
using System.Text.Json;
using Bento.Api.Models;
using RabbitMQ.Client;

namespace Bento.Api.Services;

public interface IRabbitMqService
{
    Task PublishOrderCreatedAsync(Order order, CancellationToken cancellationToken = default);
}

public class RabbitMqService : IRabbitMqService, IDisposable
{
    private readonly ILogger<RabbitMqService> _logger;
    private readonly ConnectionFactory _factory;
    private readonly string _queueName;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly object _sync = new();

    public RabbitMqService(IConfiguration configuration, ILogger<RabbitMqService> logger)
    {
        _logger = logger;

        var section = configuration.GetSection("RabbitMq");
        _factory = new ConnectionFactory
        {
            HostName = section["Host"] ?? "rabbitmq",
            Port = section.GetValue<int?>("Port") ?? 5672,
            UserName = section["UserName"] ?? "guest",
            Password = section["Password"] ?? "guest"
        };

        _queueName = section["QueueName"] ?? "order-created";
    }

    public Task PublishOrderCreatedAsync(Order order, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            EnsureChannel();

            var payload = JsonSerializer.Serialize(new
            {
                order.Id,
                order.UserId,
                order.TotalAmount,
                order.Status,
                order.OrderedAt
            });

            var body = Encoding.UTF8.GetBytes(payload);
            var properties = _channel!.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(
                exchange: string.Empty,
                routingKey: _queueName,
                basicProperties: properties,
                body: body);
        }

        _logger.LogInformation("已推送訂單訊息至 RabbitMQ，OrderId: {OrderId}", order.Id);
        return Task.CompletedTask;
    }

    // 延遲建立連線，並在連線中斷時自動重連
    private void EnsureChannel()
    {
        if (_channel is { IsOpen: true })
            return;

        _channel?.Dispose();
        _connection?.Dispose();

        _connection = _factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.QueueDeclare(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
