using System.Text;
using System.Text.Json;
using Bento.Api.Models;
using RabbitMQ.Client;

namespace Bento.Api.Services;

public interface IRabbitMqService
{
    Task PublishOrderCreatedAsync(Order order, CancellationToken cancellationToken = default);
}

public class RabbitMqService : IRabbitMqService
{
    private readonly ILogger<RabbitMqService> _logger;
    private readonly ConnectionFactory _factory;
    private readonly string _queueName;

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
        var payload = JsonSerializer.Serialize(new
        {
            order.Id,
            order.UserId,
            order.TotalAmount,
            order.Status,
            order.OrderedAt
        });

        using var connection = _factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var body = Encoding.UTF8.GetBytes(payload);
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: _queueName,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("已推送訂單訊息至 RabbitMQ，OrderId: {OrderId}", order.Id);

        return Task.CompletedTask;
    }
}
