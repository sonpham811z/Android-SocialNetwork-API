using Friend.API.Grpc;
using Friend.Application.Interfaces;
using Friend.Application.Services;
using Friend.Domain.Interfaces;
using Friend.Infrastructure.Data;
using Friend.Infrastructure.Messaging;
using Friend.Infrastructure.Repositories;
using Friend.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

// ── 1. Setup Serilog Bootstrap (Tạm thời để bắt lỗi lúc app chưa build xong) ──
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Friend.API starting up...");
    
    var builder = WebApplication.CreateBuilder(args);
    var config = builder.Configuration;

    // ── Kestrel: HTTP/2 only on 5176 to avoid HTTP_1_1_REQUIRED on cleartext gRPC ──
    // Http1AndHttp2 on a cleartext port causes the server to accept the HTTP/2
    // preface then send GOAWAY(HTTP_1_1_REQUIRED) before any data flows.
    // Http2 only eliminates the negotiation ambiguity; REST controllers and gRPC
    // both work fine over HTTP/2.
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(5176, o => o.Protocols = HttpProtocols.Http2);   // gRPC
        options.ListenAnyIP(5178, o => o.Protocols = HttpProtocols.Http1);   // REST / Swagger
    });

    // ── 2. Cấu hình Serilog chính thức (Đọc từ appsettings.json) ──
    builder.Host.UseSerilog((context, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration) 
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", "Friend.API")
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/friend-service-.txt",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            retainedFileCountLimit: 7)
    );

    Log.Information("Configuration loaded");

    // ── Database ───────────────────────────────────────────────────────────────────
    builder.Services.AddDbContext<FriendDbContext>(options =>
        options.UseNpgsql(config.GetConnectionString("DefaultConnection")));
    
    Log.Debug("Database context configured");

    // ── RabbitMQ ───────────────────────────────────────────────────────────────
    builder.Services.Configure<RabbitMQSettings>(config.GetSection("RabbitMQ"));
    builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
    Log.Debug("RabbitMQ message publisher configured");

    // ── HTTP clients ───────────────────────────────────────────────────────────
    builder.Services.AddHttpClient<IUserProfileHttpClient, UserProfileHttpClient>();
    Log.Debug("HTTP clients configured");

    // ── Repositories & UoW ────────────────────────────────────────────────────
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    Log.Debug("Repository pattern and UnitOfWork configured");

    // ── Application services ───────────────────────────────────────────────────
    builder.Services.AddScoped<IFriendService, FriendService>();
    builder.Services.AddScoped<IFriendRequestService, FriendRequestService>();
    builder.Services.AddScoped<IFollowService, FollowService>();
    builder.Services.AddScoped<IBlockService, BlockService>();
    Log.Debug("Application services registered");

    // ── Auth ───────────────────────────────────────────────────────────────
    var jwtSecret = config["Jwt:Key"] ?? config["Jwt:SecretKey"];
    if (string.IsNullOrWhiteSpace(jwtSecret))
    {
        Log.Fatal("JWT signing key is missing. Configure Jwt:Key or Jwt:SecretKey");
        throw new InvalidOperationException("JWT signing key is missing. Configure Jwt:Key or Jwt:SecretKey.");
    }

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
                                               Encoding.UTF8.GetBytes(jwtSecret))
            };
        });

    Log.Debug("JWT Bearer authentication configured");

    // ── gRPC ────────────────────────────────────────────────────────────────
    builder.Services.AddGrpc();

    builder.Services.AddAuthorization();
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    Log.Information("Application pipeline configured");

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        Log.Debug("Swagger UI enabled for Development environment");
    }

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapGrpcService<FriendshipGrpcService>();

    // Auto-migrate on startup (remove in production; use proper migration pipeline)
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<FriendDbContext>();
        Log.Information("Database context retrieved for migration check");
        // await db.Database.MigrateAsync();
    }

    Log.Information("Friend.API started successfully. Listening on specified port");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Friend.API terminated unexpectedly");
}
finally
{
    Log.Information("Friend.API shutting down");
    Log.CloseAndFlush(); // Ở phiên bản Serilog hiện tại, hàm này thường là đồng bộ (không có await)
}