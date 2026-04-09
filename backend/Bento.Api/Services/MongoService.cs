using Bento.Api.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Bento.Api.Services;

public interface IMongoService
{
    Task LogOrderAsync(Order order, CancellationToken cancellationToken = default);
}

public class MongoService : IMongoService
{
    private readonly IMongoCollection<BsonDocument> _collection;

    public MongoService(IConfiguration configuration)
    {
        var section = configuration.GetSection("Mongo");
        var connectionString = section["ConnectionString"] ?? "mongodb://bento:password@mongo:27017";
        var databaseName = section["Database"] ?? "bento_logs";
        var collectionName = section["OrderCollection"] ?? "order_logs";

        var client = new MongoClient(connectionString);
        _collection = client.GetDatabase(databaseName).GetCollection<BsonDocument>(collectionName);
    }

    public async Task LogOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        var items = new BsonArray(order.Items.Select(item => new BsonDocument
        {
            { "menuItemId", item.MenuItemId },
            { "quantity", item.Quantity },
            { "unitPrice", item.UnitPrice }
        }));

        var document = new BsonDocument
        {
            { "orderId", order.Id },
            { "userId", order.UserId },
            { "status", order.Status },
            { "totalAmount", order.TotalAmount },
            { "orderedAt", order.OrderedAt },
            { "items", items },
            { "loggedAt", DateTime.UtcNow }
        };

        await _collection.InsertOneAsync(document, cancellationToken: cancellationToken);
    }
}
