namespace Bento.Api.Constants;

public static class OutboxMessageTypes
{
    public const string OrderCreatedRabbitMq = "order-created:rabbitmq";
    public const string OrderCreatedMongoLog = "order-created:mongo-log";
}
