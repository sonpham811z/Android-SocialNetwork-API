using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Notification.Application.Interfaces;
using Notification.Application.Services;
using Notification.Domain.Interfaces;
using Notification.Infrastructure.Data;
using Notification.Infrastructure.Hubs;
using Notification.Infrastructure.Messaging;
using Notification.Infrastructure.Repositories;
using Notification.Infrastructure.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var config  = builder.Configuration;

// ── Firebase Admin SDK ────────────────────────────────────────────────────────
var credentialsPath = config["Firebase:CredentialsPath"];
if (!string.IsNullOrEmpty(credentialsPath) && File.Exists(credentialsPath))
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(credentialsPath)
    });
}
else
{
    // Fallback: use GOOGLE_APPLICATION_CREDENTIALS env variable
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.GetApplicationDefault()
    });
}

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(config.GetConnectionString("DefaultConnection")));

// ── RabbitMQ ──────────────────────────────────────────────────────────────────
builder.Services.Configure<RabbitMQSettings>(config.GetSection("RabbitMQ"));
builder.Services.AddHostedService<RabbitMQEventConsumer>();

// ── Repositories & UoW ───────────────────────────────────────────────────────
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ── Application Services ──────────────────────────────────────────────────────
builder.Services.AddScoped<INotificationService, NotificationService>();

// ── Infrastructure Services ───────────────────────────────────────────────────
builder.Services.AddSingleton<IOnlineTracker, OnlineTracker>();   // singleton: shared state
builder.Services.AddScoped<IFcmService, FcmService>();
builder.Services.AddScoped<IRealtimeService, SignalRRealtimeService>();

// ── HTTP Clients ──────────────────────────────────────────────────────────────
builder.Services.AddHttpClient<IPostHttpClient, PostHttpClient>(client =>
{
    client.BaseAddress = new Uri(config["Services:PostServiceUrl"]
                                 ?? "http://localhost:5003");
});

// ── SignalR ───────────────────────────────────────────────────────────────────
// Serialize enums as their string names (e.g. "FriendRequestSent", "Read") so the
// Flutter client can parse them — default System.Text.Json emits enums as numbers.
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
        options.PayloadSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));

// ── Auth ──────────────────────────────────────────────────────────────────────
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
                                           Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
            ClockSkew                = TimeSpan.Zero
        };

        // SignalR sends the JWT token via query string when using WebSockets/SSE
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path        = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers()
    // Emit enums as string names ("FriendRequestSent", "Read") instead of numbers,
    // matching what the Flutter client expects when parsing NotificationDto.
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // required for SignalR
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notification");

// ── Auto-migrate ──────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
