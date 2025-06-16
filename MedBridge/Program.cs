using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.FileProviders;
using System.Text;
using System.Net.WebSockets;
using MedBridge.Models;
using MedBridge.Services;
using System.Collections.Concurrent;
using Stripe;
using Microsoft.Extensions.Logging;
using MoviesApi.models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Stripe.Climate;
using GraduationProject.Core.Interfaces;
using GraduationProject.Core.Services;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Configure logging
    builder.Services.AddLogging(logging =>
    {
        logging.AddConsole();
        logging.AddDebug();
        logging.SetMinimumLevel(LogLevel.Debug);
    });

    // Ensure configuration is loaded
    builder.Configuration
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();

    // Log configuration loading
    var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Loading configuration from appsettings.json at {Path}", Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));

    // Connection string
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    logger.LogInformation("Connection string: {ConnectionString}", connectionString);
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));

    // Email settings
    builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
    builder.Services.AddTransient<EmailService>();
    builder.Services.AddScoped<IDeliveryPersonService, DeliveryPersonService>();    // Custom services
    builder.Services.AddScoped<CartService>();
    builder.Services.AddScoped<IGoogleSignIn, GoogleSignIn>();
    builder.Services.AddMemoryCache();
    builder.Services.AddScoped<RecommendationService>();
    builder.Services.AddHttpClient(); // 👈 أضف السطر ده
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

    // Configure Stripe API key
    var stripeKey = builder.Configuration.GetValue<string>("STRIPE_SECRET_KEY");
    logger.LogInformation("STRIPE_SECRET_KEY loaded: {Key}", string.IsNullOrEmpty(stripeKey) ? "null" : "set");
    if (string.IsNullOrEmpty(stripeKey))
    {
        logger.LogError("STRIPE_SECRET_KEY is not configured in appsettings.json or environment variables.");
        throw new InvalidOperationException("STRIPE_SECRET_KEY is not configured.");
    }
    StripeConfiguration.ApiKey = stripeKey;

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
    app.Map("/ws", async context =>
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("WebSocket request expected.");
            return;
        }

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
    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    logger.LogInformation("Starting application on https://10.0.2.2:7273");
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Application failed to start: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    throw;
}
