using System.Text;
using Message.Application.Interfaces;
using Message.Application.Services;
using Message.Domain.Interfaces;
using Message.Infrastructure.Data;
using Message.Infrastructure.Hubs;
using Message.Infrastructure.Messaging;
using Message.Infrastructure.Repositories;
using Message.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var config  = builder.Configuration;

// ── MongoDB ───────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<MongoDbContext>();

// ── Redis ─────────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(config.GetConnectionString("Redis")
        ?? throw new InvalidOperationException("Redis connection string is not configured.")));

// ── Repositories ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();

// ── Application Services ──────────────────────────────────────────────────────
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IMessageService, MessageService>();

// ── Infrastructure Services ───────────────────────────────────────────────────
builder.Services.AddSingleton<IOnlineStatusService, RedisOnlineStatusService>();
builder.Services.AddSingleton<IConversationCacheService, RedisConversationCacheService>();
builder.Services.AddScoped<ISignalRMessageService, SignalRMessageService>();

// Publisher is singleton because the RabbitMQ channel is reused across requests
builder.Services.AddSingleton<IMessageEventPublisher, RabbitMQMessagePublisher>();

// ── RabbitMQ Settings ─────────────────────────────────────────────────────────
builder.Services.Configure<RabbitMQSettings>(config.GetSection("RabbitMQ"));

// ── gRPC Client → Friend Service ──────────────────────────────────────────────
// GrpcServices="Client" in Message.Infrastructure.csproj generates FriendshipService.FriendshipServiceClient
// from Protos/friendship.proto. Friend Service must implement the server side.
builder.Services.AddGrpcClient<Message.Infrastructure.GrpcProtos.FriendshipService.FriendshipServiceClient>(o =>
{
    o.Address = new Uri(config["GrpcServices:FriendService"]
        ?? throw new InvalidOperationException("GrpcServices:FriendService is not configured."));
});
builder.Services.AddScoped<IFriendServiceClient, GrpcFriendServiceClient>();

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── JWT Authentication ────────────────────────────────────────────────────────
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = config["Jwt:Issuer"],
            ValidAudience            = config["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
            ClockSkew = TimeSpan.Zero
        };

        // Allow SignalR to receive JWT via query string (?access_token=...)
        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"].ToString();
                if (!string.IsNullOrEmpty(token) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    ctx.Token = token;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Message Service API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Name   = "Authorization",
        Type   = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            []
        }
    });
});

// ── Global exception handling ─────────────────────────────────────────────────
builder.Services.AddProblemDetails();

var app = builder.Build();

// ── Ensure MongoDB indexes on startup ─────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var msgRepo = scope.ServiceProvider.GetRequiredService<IMessageRepository>();
    await msgRepo.CreateIndexesAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// SignalR hub route (JWT via ?access_token= supported by OnMessageReceived above)
app.MapHub<MessageHub>("/hubs/message");

app.Run();
