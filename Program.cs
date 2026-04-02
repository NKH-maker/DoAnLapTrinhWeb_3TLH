using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Hangfire;
using Hangfire.SqlServer;
using Serilog;
using System.Text;
using TINH_FINAL_2256.Models;
using TINH_FINAL_2256.Repositories;
using TINH_FINAL_2256.Services;
using TINH_FINAL_2256.Hubs;

// C?u hình Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

try
{
    // Serilog
    builder.Host.UseSerilog();

    // Distributed Memory Cache
    builder.Services.AddDistributedMemoryCache();

    // Redis Caching
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("RedisConnection") ?? "localhost:6379";
    });

    // Session
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    // Controllers & Views
    builder.Services.AddControllersWithViews();
    builder.Services.AddRazorPages();

    // Database
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Identity
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
        .AddDefaultTokenProviders()
        .AddEntityFrameworkStores<ApplicationDbContext>();

    // Cookie Authentication
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

    // JWT Authentication
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var jwtKey = jwtSettings["Key"] ?? "your-default-secret-key-min-32-characters";
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    })
    // OAuth Google
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    });

    // RabbitMQ
    builder.Services.AddSingleton<RabbitMQ.Client.IConnectionFactory>(sp =>
    {
        var factory = new RabbitMQ.Client.ConnectionFactory()
        {
            HostName = builder.Configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = int.TryParse(builder.Configuration["RabbitMQ:Port"], out var port) ? port : 5672,
            UserName = builder.Configuration["RabbitMQ:UserName"] ?? "guest",
            Password = builder.Configuration["RabbitMQ:Password"] ?? "guest",
            DispatchConsumersAsync = true
        };
        return factory;
    });

    // Hangfire
    builder.Services.AddHangfire(configuration =>
    {
        configuration.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"));
    });
    builder.Services.AddHangfireServer();

    // SignalR
    builder.Services.AddSignalR();

    // Repositories
    builder.Services.AddScoped<IProductRepository, EFProductRepository>();
    builder.Services.AddScoped<ICategoryRepository, EFCategoryRepository>();

    // Services
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IMessageQueueService, RabbitMQService>();
    builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();
    builder.Services.AddScoped<IQRCodeService, QRCodeService>();
    builder.Services.AddScoped<IExcelService, ExcelService>();

    var app = builder.Build();

    // Initialize Roles
    using (var scope = app.Services.CreateScope())
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var roles = new[] { SD.Role_Admin, SD.Role_Customer, SD.Role_Company, SD.Role_Employee };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    // Middleware
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
    }

    app.UseStaticFiles();
    app.UseSession();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    // Hangfire Dashboard
    app.UseHangfireDashboard();

    // Routes
    app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
    app.MapRazorPages();

    // SignalR Hubs
    // app.MapHub<YourHub>("/hub/yourHub");
    app.MapHub<NotificationHub>("/hub/notifications");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}