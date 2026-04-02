using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.SqlServer;
using Hangfire.Dashboard;
using Serilog;
using TINH_FINAL_2256.Models;
using TINH_FINAL_2256.Repositories;
using TINH_FINAL_2256.Services;
using TINH_FINAL_2256.Hubs;

// Configure Serilog (console only to reduce file I/O during development)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

try
{
    // Serilog
    builder.Host.UseSerilog();

    // Distributed Memory Cache (use in development)
    builder.Services.AddDistributedMemoryCache();

    // Redis Caching - enable only in Production to avoid extra connection overhead during development
    if (builder.Environment.IsProduction())
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration.GetConnectionString("RedisConnection") ?? "localhost:6379";
        });
    }

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

    // External providers (Google) - keep social login support without adding JwtBearer middleware
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
            options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
        });

    // Hangfire (background jobs) - keep for reliable internal scheduling
    builder.Services.AddHangfire(configuration =>
    {
        configuration.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"));
    });
    builder.Services.AddHangfireServer();

    // SignalR (real-time) - keep for real-time notifications and chat
    builder.Services.AddSignalR();

    // Repositories
    builder.Services.AddScoped<IProductRepository, EFProductRepository>();
    builder.Services.AddScoped<ICategoryRepository, EFCategoryRepository>();

    // Services
    builder.Services.AddScoped<IEmailService, EmailService>();
    // Replace RabbitMQ-backed IMessageQueueService with a lightweight null implementation to avoid external MQ dependency
    builder.Services.AddScoped<IMessageQueueService, NullMessageQueueService>();
    builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();
    builder.Services.AddScoped<IQRCodeService, QRCodeService>();
    builder.Services.AddScoped<IExcelService, ExcelService>();
    builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();

    // Hangfire dashboard authorization filter registration
    builder.Services.AddSingleton<IDashboardAuthorizationFilter, HangfireAuthorizationFilter>();

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

    // Seed sample data (orders)
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataSeeder>>();
        
        var seeder = new TINH_FINAL_2256.Services.DataSeeder(context, userManager, logger);
        await seeder.SeedSampleOrdersAsync();
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

    // Hangfire Dashboard - protected by authorization filter
    var dashAuth = app.Services.GetRequiredService<IDashboardAuthorizationFilter>();
    app.UseHangfireDashboard(builder.Configuration["Hangfire:DashboardPath"] ?? "/hangfire", new DashboardOptions
    {
        Authorization = new[] { dashAuth }
    });

    // Routes
    app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
    app.MapRazorPages();

    // SignalR Hubs
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