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
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GraduationProject.Core.Interfaces;
using GraduationProject.Core.Services;
using MedBridge.Services.UserService;
using MedBridge.Services.PaymentService;
using RatingApi.Services;
using CouponSystemApi.Services;
using CloudinaryDotNet;
using Account = CloudinaryDotNet.Account;
using MoviesApi.models;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Configure logging
    builder.Services.AddLogging(logging =>
    {
        logging.AddConsole();
        logging.AddDebug();
        logging.SetMinimumLevel(builder.Environment.IsDevelopment() ? LogLevel.Debug : LogLevel.Information);
    });

    // Ensure configuration is loaded
    builder.Configuration
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables();

    // Log configuration loading
    var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Loading configuration from appsettings.json at {Path}", Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));

    // Connection string
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        logger.LogError("Database connection string is not configured.");
        throw new InvalidOperationException("Database connection string is not configured.");
    }
    logger.LogInformation("Connection string loaded successfully.");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString)
               .EnableSensitiveDataLogging()
               .EnableDetailedErrors());

    // Cloudinary configuration
    var cloudinaryConfig = builder.Configuration.GetSection("Cloudinary");
    var cloudName = cloudinaryConfig["CloudName"];
    var apiKey = cloudinaryConfig["ApiKey"];
    var apiSecret = cloudinaryConfig["ApiSecret"];
    if (builder.Environment.IsProduction() &&
        (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret)))
    {
        logger.LogError("Cloudinary configuration is missing or incomplete.");
        throw new InvalidOperationException("Cloudinary configuration (CloudName, ApiKey, ApiSecret) is not configured.");
    }
    if (!string.IsNullOrEmpty(cloudName) && !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret))
    {
        builder.Services.AddSingleton<Cloudinary>(sp =>
        {
            var account = new Account(cloudName, apiKey, apiSecret);
            return new Cloudinary(account);
        });
        builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
    }
    else
    {
        logger.LogWarning("Cloudinary configuration skipped in non-production environment.");
    }

    // Email settings
    builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
    builder.Services.AddTransient<EmailService>();

    // Register services
    builder.Services.AddScoped<ICartServicee, CartServicee>();
    builder.Services.AddScoped<CartService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IProductService, MedBridge.Services.ProductService>();
    builder.Services.AddScoped<ICategoryService, CategoryService>();
    builder.Services.AddScoped<ISubcategoryService, SubcategoryService>();
    builder.Services.AddScoped<IDeliveryPersonAdminService, DeliveryPersonAdminService>();
    builder.Services.AddScoped<IDeliveryPersonService, DeliveryPersonService>();
    builder.Services.AddScoped<IForgotPasswordService, ForgotPasswordService>();
    builder.Services.AddScoped<IPaymentService, PaymentService>();
    builder.Services.AddScoped<ISpecialtiesService, SpecialtiesService>();
    builder.Services.AddScoped<IWorkTypesService, WorkTypesService>();
    builder.Services.AddScoped<IRatingService, RatingService>();
    builder.Services.AddScoped<IShippingPriceService, ShippingPriceService>();
    builder.Services.AddScoped<IFavouritesService, FavouritesService>();
    builder.Services.AddScoped<IDashboardService, DashboardService>();
    builder.Services.AddScoped<IOrderService, OrderServicee>();
    builder.Services.AddScoped<IAdminService, AdminService>();
    builder.Services.AddScoped<CustomerService>();
    builder.Services.AddScoped<TokenService>();
    builder.Services.AddScoped<ChargeService>();
    builder.Services.AddScoped<IContactUsService, ContactUsService>();
    builder.Services.AddScoped<ICouponService, CouponServicee>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IGoogleSignIn, GoogleSignIn>();
    builder.Services.AddScoped<RecommendationService>();
    builder.Services.AddMemoryCache();
    builder.Services.AddHttpClient();

    // Configure Stripe API key
    var stripeKey = builder.Configuration.GetValue<string>("STRIPE_SECRET_KEY");
    logger.LogInformation("STRIPE_SECRET_KEY loaded: {Key}", string.IsNullOrEmpty(stripeKey) ? "null" : "set");
    if (builder.Environment.IsProduction() && string.IsNullOrEmpty(stripeKey))
    {
        logger.LogError("STRIPE_SECRET_KEY is not configured.");
        throw new InvalidOperationException("STRIPE_SECRET_KEY is not configured.");
    }
    if (!string.IsNullOrEmpty(stripeKey))
    {
        StripeConfiguration.ApiKey = stripeKey;
    }
    else
    {
        logger.LogWarning("Stripe configuration skipped in non-production environment.");
    }

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
        logger.LogInformation("WebSocket connected: {Username}", username);

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
        logger.LogInformation("WebSocket disconnected: {Username}", username);
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

    logger.LogInformation("Starting application on Render with dynamic port");
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Application failed to start: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    throw;
}