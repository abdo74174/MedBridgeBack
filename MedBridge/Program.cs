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
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using MedbridgeApi.Services;

internal class Program
{
    private static void Main(string[] args)
    {
        try
        {
            var builder = WebApplication.CreateBuilder(args);

            // Enhanced Logging Configuration
            builder.Services.AddLogging(logging =>
            {
                logging.AddConsole();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddFilter("Microsoft", LogLevel.Warning);
                logging.AddFilter("System", LogLevel.Warning);
            });
            StripeConfiguration.ApiKey = builder.Configuration["STRIPE_SECRET_KEY"];

            // Config
            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Application starting in {Environment} mode", builder.Environment.EnvironmentName);

            // SQL Server Connection with better error handling
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                logger.LogCritical("Database connection string is not configured.");
                throw new InvalidOperationException("Database connection string is not configured.");
            }

            try
            {
                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(connectionString)
                           .EnableSensitiveDataLogging()
                           .EnableDetailedErrors());
                logger.LogInformation("Database configured successfully");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Failed to configure database");
                throw;
            }

            // Add Controllers
            builder.Services.AddControllers();

            // Cloudinary with better error handling
            try
            {
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
                    builder.Services.AddSingleton(sp =>
                    {
                        var account = new Account(cloudName, apiKey, apiSecret);
                        return new Cloudinary(account);
                    });
                    builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
                    logger.LogInformation("Cloudinary service configured");
                }
                else
                {
                    logger.LogWarning("Cloudinary configuration skipped in non-production environment.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error configuring Cloudinary");
                if (builder.Environment.IsProduction()) throw;
            }

            // Email + Services
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
            builder.Services.AddTransient<EmailService>();

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
            builder.Services.AddScoped<IBuyerRequestService, BuyerRequestService>();

            builder.Services.AddMemoryCache();
            builder.Services.AddHttpClient();

            // JWT Auth
            var jwtKey = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);
            try
            {
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
                            IssuerSigningKey = new SymmetricSecurityKey(jwtKey)
                        };
                    });
                logger.LogInformation("JWT Authentication configured");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Failed to configure JWT Authentication");
                throw;
            }

            builder.Services.AddAuthorization();

            // CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                });
            });
            logger.LogInformation("CORS policy configured");

            // Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "MedBridge API", Version = "v1", Description = "API for MedBridge Project" });
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
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        new string[] { }
                    }
                });
            });

            var app = builder.Build();

            // Enhanced Firebase initialization
            try
            {
                var firebaseConfig = builder.Configuration.GetSection("Firebase");
                var firebaseJson = firebaseConfig["ServiceAccountJson"];
                var projectId = firebaseConfig["ProjectId"];

                if (string.IsNullOrEmpty(firebaseJson) || string.IsNullOrEmpty(projectId))
                {
                    logger.LogCritical("Firebase configuration is missing (ServiceAccountJson or ProjectId)");
                    throw new InvalidOperationException("Firebase configuration is missing");
                }

                if (FirebaseApp.DefaultInstance == null)
                {
                    AppOptions appOptions;
                    if (firebaseJson.TrimStart().StartsWith("{"))
                    {
                        // JSON string provided
                        logger.LogInformation("Using Firebase JSON string from configuration");
                        try
                        {
                            appOptions = new AppOptions
                            {
                                Credential = GoogleCredential.FromJson(firebaseJson),
                                ProjectId = projectId
                            };
                        }
                        catch (Exception ex)
                        {
                            logger.LogCritical(ex, "Failed to parse Firebase JSON string");
                            throw new InvalidOperationException("Invalid Firebase JSON string", ex);
                        }
                    }
                    else
                    {
                        // JSON file path provided
                        var firebaseJsonFullPath = Path.GetFullPath(firebaseJson);
                        logger.LogInformation("Looking for Firebase JSON at: {Path}", firebaseJsonFullPath);
                        if (!System.IO.File.Exists(firebaseJsonFullPath))
                        {
                            logger.LogCritical("Firebase JSON file not found at: {Path}", firebaseJsonFullPath);
                            throw new FileNotFoundException("Firebase JSON file not found", firebaseJsonFullPath);
                        }
                        appOptions = new AppOptions
                        {
                            Credential = GoogleCredential.FromFile(firebaseJsonFullPath),
                            ProjectId = projectId
                        };
                    }

                    FirebaseApp.Create(appOptions);
                    logger.LogInformation("Firebase initialized successfully");
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "FATAL ERROR: Failed to initialize Firebase");
                throw;
            }

            // WebSocket configuration
            app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(120) });
            var users = new ConcurrentDictionary<string, WebSocket>();

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
                    var handler = new JwtSecurityTokenHandler();
                    principal = handler.ValidateToken(token, new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(jwtKey)
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
                                    CancellationToken.None);
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

            // Swagger only in development
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                logger.LogInformation("Swagger UI enabled for development environment");
            }

            // Static files
            try
            {
                var staticFilesPath = Path.Combine(Directory.GetCurrentDirectory(), "assets", "images");
                if (!Directory.Exists(staticFilesPath))
                {
                    logger.LogWarning("Static files directory not found: {Path}", staticFilesPath);
                }
                else
                {
                    app.UseStaticFiles(new StaticFileOptions
                    {
                        FileProvider = new PhysicalFileProvider(staticFilesPath),
                        RequestPath = "/images"
                    });
                    logger.LogInformation("Static files configured at: {Path}", staticFilesPath);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error configuring static files");
            }
            app.UseSwagger();
            app.UseSwaggerUI();

            // Middleware pipeline
            app.UseCors("AllowAll");
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            logger.LogInformation("Application startup completed");
            app.Run();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Application failed to start: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();
            throw;
        }
    }
}