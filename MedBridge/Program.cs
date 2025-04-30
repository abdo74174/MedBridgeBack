using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.FileProviders;
using System.Text;
using System.Net;
using System.Net.WebSockets;
using MedBridge.Models;
using MedBridge.Services;
using MoviesApi.models;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<EmailService>();
// Custom services
builder.Services.AddScoped<CartService>();
builder.Services.AddMemoryCache();
// Controllers & JWT Auth
builder.Services.AddControllers();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)

    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MedBridge API",
        Version = "v1",
        Description = "API for MedBridge Project"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token like: Bearer {your_token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// Build app
var app = builder.Build();

// WebSocket config
var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(120),
};
app.UseWebSockets(webSocketOptions);

// Store connected sockets
var users = new ConcurrentDictionary<string, WebSocket>();

// WebSocket endpoint
// WebSocket endpoint
app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("WebSocket request expected.");
        return;
    }

    // ✅ Extract JWT
    var token = context.Request.Headers["Authorization"].ToString()?.Replace("Bearer ", "");
    if (string.IsNullOrEmpty(token))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Missing or invalid token.");
        return;
    }

    ClaimsPrincipal principal;
    try
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);
        principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        }, out _);
    }
    catch
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Unauthorized");
        return;
    }

    var username = principal.Identity?.Name ?? "Unknown";

    // ✅ Accept WebSocket
    var socket = await context.WebSockets.AcceptWebSocketAsync();
    var userId = Guid.NewGuid().ToString();
    users.TryAdd(userId, socket);
    Console.WriteLine($"✅ Connected: {username}");

    var buffer = new byte[1024 * 4];
    while (socket.State == WebSocketState.Open)
    {
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        if (result.MessageType == WebSocketMessageType.Text)
        {
            var messageText = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var fullMessage = $"{username}: {messageText}";

            // ✅ Save to DB
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.ChatMessages.Add(new ChatMessage
                {
                    Username = username,
                    Message = messageText,
                    Timestamp = DateTime.Now
                });
                await db.SaveChangesAsync();
            }

            // ✅ Broadcast message
            foreach (var user in users.Values)
            {
                if (user.State == WebSocketState.Open)
                {
                    await user.SendAsync(
                        new ArraySegment<byte>(Encoding.UTF8.GetBytes(fullMessage)),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );
                }
            }
        }
        else if (result.MessageType == WebSocketMessageType.Close)
        {
            break;
        }
    }

    users.TryRemove(userId, out _);
    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
    Console.WriteLine($"❌ Disconnected: {username}");
});

// Dev tools
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Static files (images)
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "assets", "images")),
    RequestPath = "/images"
});

// Pipeline
app.UseHttpsRedirection();


// Then in app configuration
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
