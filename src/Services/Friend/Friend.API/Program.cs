using Friend.Application.Interfaces;
using Friend.Application.Services;
using Friend.Domain.Interfaces;
using Friend.Infrastructure.Data;
using Friend.Infrastructure.Messaging;
using Friend.Infrastructure.Repositories;
using Friend.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var config  = builder.Configuration;

// ── Database ───────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<FriendDbContext>(options =>
    options.UseNpgsql(config.GetConnectionString("DefaultConnection")));

// ── Redis ──────────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(config["Redis:ConnectionString"] ?? "localhost:6379"));
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// ── RabbitMQ ───────────────────────────────────────────────────────────────────
builder.Services.Configure<RabbitMQSettings>(config.GetSection("RabbitMQ"));
builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

// ── HTTP clients ───────────────────────────────────────────────────────────────
builder.Services.AddHttpClient<IUserProfileHttpClient, UserProfileHttpClient>();

// ── Repositories & UoW ────────────────────────────────────────────────────────
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ── Application services ───────────────────────────────────────────────────────
builder.Services.AddScoped<IFriendService, FriendService>();
builder.Services.AddScoped<IFriendRequestService, FriendRequestService>();
builder.Services.AddScoped<IFollowService, FollowService>();
builder.Services.AddScoped<IBlockService, BlockService>();

// ── Auth ───────────────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = config["Jwt:Issuer"],
            ValidAudience            = config["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(config["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Auto-migrate on startup (remove in production; use proper migration pipeline)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FriendDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();