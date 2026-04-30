using System.Text.Json.Serialization;
using Bento.Api.Data;
using Bento.Api.Services;
using Bento.Api.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var postgresConnection = builder.Configuration.GetConnectionString("PostgreSql")
    ?? "Host=postgres;Port=5432;Database=bentodb;Username=bento;Password=bento123";

builder.Services.AddDbContext<BentoDbContext>(options => options.UseNpgsql(postgresConnection));

builder.Services.AddCors(options =>
{
    options.AddPolicy("BentoCors", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (origins is { Length: > 0 })
        {
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
            return;
        }

        if (builder.Environment.IsProduction())
            throw new InvalidOperationException("正式環境必須在 Cors:AllowedOrigins 設定允許的來源清單。");

        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var redisConnection = builder.Configuration.GetSection("Redis")["ConnectionString"]
    ?? "redis:6379,password=redis123";

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
builder.Services.AddScoped<IRedisService, RedisService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// Singleton：持久連線，重用 RabbitMQ channel；MongoClient 本身為 thread-safe 設計
builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();
builder.Services.AddSingleton<IMongoService, MongoService>();
builder.Services.AddHostedService<OutboxDispatcherService>();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<OrderValidator>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("BentoCors");
app.MapControllers();

app.MapGet("/", () => Results.Ok(new { message = "Bento API running" }));

app.Run();
