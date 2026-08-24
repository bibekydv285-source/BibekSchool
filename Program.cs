using BibekSchool.Data;
using BibekSchool.Models;
using BibekSchool.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Configure DbContext with Azure SQL resilience
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // Azure SQL transient fault handling
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(60);
    });
    
    // Only enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configure Email settings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<PasswordResetSettings>(builder.Configuration.GetSection("PasswordReset"));

// Register Email Service
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    // Sign in settings
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure application cookie for production
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Always HTTPS in production
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ReturnUrlParameter = "returnUrl";

    // Prevent caching of authenticated pages - critical for logout security
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, private";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, private";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
        return Task.CompletedTask;
    };
});

// Add cache control for authenticated responses
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<NoCacheForAuthenticatedFilter>();
})
.AddRazorRuntimeCompilation();

builder.Services.AddRazorPages();

builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ITeacherService, TeacherService>();
builder.Services.AddScoped<IMarkService, MarkService>();
builder.Services.AddScoped<IResultService, ResultService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

var app = builder.Build();

// Seed database with error handling
using (var scope = app.Services.CreateScope())
{
    try
    {
        await DbSeeder.SeedAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database seeding failed");
        // Don't crash on seeding failure in production
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    
    // Forward proxy headers for Azure App Service
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                          Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Add security headers middleware
app.Use(async (context, next) =>
{
    // Prevent caching of authenticated pages to prevent back-button access after logout
    if (context.User.Identity?.IsAuthenticated == true)
    {
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, private";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
    }

    // Security headers
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; font-src 'self' https://cdnjs.cloudflare.com; img-src 'self' data: https:; connect-src 'self'";

    await next();
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapRazorPages();

app.Run();

// Filter to prevent caching of authenticated pages
public class NoCacheForAuthenticatedFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            context.HttpContext.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, private";
            context.HttpContext.Response.Headers["Pragma"] = "no-cache";
            context.HttpContext.Response.Headers["Expires"] = "0";
        }
        await next();
    }
}