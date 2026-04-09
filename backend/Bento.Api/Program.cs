using System.Text.Json.Serialization;
using Bento.Api.Data;
using Bento.Api.Services;
using Bento.Api.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// 路由規則統一小寫，並搭配 Controller 上的 id:int:min(1) 進行限制
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

        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var redisConnection = builder.Configuration.GetSection("Redis")["ConnectionString"]
    ?? "redis:6379,password=redis123";

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
builder.Services.AddScoped<IRedisService, RedisService>();
builder.Services.AddScoped<IRabbitMqService, RabbitMqService>();
builder.Services.AddScoped<IMongoService, MongoService>();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<OrderValidator>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("BentoCors");
app.MapControllers();

app.MapGet("/", () => Results.Ok(new { message = "Bento API running" }));

app.Run();
