using BibekSchool.Data;
using BibekSchool.Models;
using BibekSchool.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ────────────────────────────────────────────────────────────────
// Tell ASP.NET Core to trust Azure's reverse-proxy headers.
// Azure terminates HTTPS at its load balancer and forwards requests
// to your app as plain HTTP internally. Without this, UseHttpsRedirection()
// thinks every request is HTTP and redirects forever.
// ────────────────────────────────────────────────────────────────
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Read connection string from configuration.
// Development: WebApplication.CreateBuilder auto-loads User Secrets
// (because <UserSecretsId> exists in the .csproj) — no extra code needed.
// Production (Azure App Service): reads from Configuration → Connection Strings.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    var env = builder.Environment.EnvironmentName;
    throw new InvalidOperationException(
        $"Connection string 'DefaultConnection' is not configured for environment '{env}'. " +
        "Set it via: " +
        "1. dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"...\" (development), " +
        "2. Azure Portal → App Service → Configuration → Connection Strings (Name: DefaultConnection, Type: SQLAzure) (production), " +
        "3. Environment variable 'ConnectionStrings__DefaultConnection'.");
}

var isAzureSql = connectionString.Contains(".database.windows.net", StringComparison.OrdinalIgnoreCase);
var useManagedIdentity = isAzureSql
    && !connectionString.Contains("Password=", StringComparison.OrdinalIgnoreCase)
    && !connectionString.Contains("User ID=", StringComparison.OrdinalIgnoreCase)
    && !connectionString.Contains("Uid=", StringComparison.OrdinalIgnoreCase);

// If Managed Identity is in play, append the keyword ONCE, here, at startup.
// SqlClient itself refreshes the AD token internally on every new connection —
// this avoids the "works for an hour then dies" bug from manually fetching a token.
if (useManagedIdentity)
{
    connectionString += ";Authentication=Active Directory Default";
}

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(60);
        if (isAzureSql)
        {
            sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        }
    });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<PasswordResetSettings>(builder.Configuration.GetSection("PasswordReset"));
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Custom claims factory to ensure roles are always in the authentication cookie
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, CustomUserClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ReturnUrlParameter = "returnUrl";

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

// MUST be first — before UseHttpsRedirection, UseHsts, everything.
app.UseForwardedHeaders();

// ────────────────────────────────────────────────────────────────
// Seeding failures are logged AND surfaced clearly at startup (via
// Log Stream) with a distinct, greppable message, so a broken
// connection string shows up immediately instead of only manifesting
// later as a confusing error on the Dashboard page. The app still
// doesn't crash — seeding failure alone shouldn't take the whole
// site down — but now you know right away it happened.
// ────────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await DbSeeder.SeedAsync(scope.ServiceProvider);
        logger.LogInformation("Database seeding completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "STARTUP DATABASE SEEDING FAILED — check the connection string and that the database/tables exist.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
}
else
{
    // ────────────────────────────────────────────────────────────────
    // Custom exception handler. Logs the full inner-exception chain
    // always, and — only when "Diagnostics:ShowDetailedErrors" is set
    // to "true" in Azure Configuration — writes the real exception
    // message to the response too, without switching
    // ASPNETCORE_ENVIRONMENT to Development.
    //
    // NEW: loop guard. If the page that just threw is the SAME page
    // we'd normally redirect the user's role to (e.g. Student ->
    // /Student/Dashboard, but /Student/Dashboard is what just threw),
    // redirecting again would throw again -> infinite redirect loop.
    // In that case we render a plain static HTML message instead,
    // which cannot itself throw, breaking the loop for good.
    // ────────────────────────────────────────────────────────────────
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var showDetailedErrors = app.Configuration.GetValue<bool>("Diagnostics:ShowDetailedErrors");
            var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
            var ex = exceptionHandlerFeature?.Error;
            var failedPath = exceptionHandlerFeature?.Path ?? string.Empty;
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

            if (ex != null)
            {
                var level = 0;
                var current = ex;
                while (current != null)
                {
                    logger.LogError(
                        "Unhandled exception at {Path} — [Level {Level}] {ExType}: {Message}",
                        failedPath, level, current.GetType().Name, current.Message);
                    current = current.InnerException;
                    level++;
                }
            }

            context.Response.StatusCode = 500;

            if (showDetailedErrors && ex != null)
            {
                context.Response.ContentType = "text/plain";
                var root = ex;
                while (root.InnerException != null) root = root.InnerException;
                await context.Response.WriteAsync(
                    $"DEBUG (temporary — disable Diagnostics:ShowDetailedErrors when done)\n\n" +
                    $"Path: {failedPath}\n" +
                    $"Type: {root.GetType().FullName}\n" +
                    $"Message: {root.Message}\n\n" +
                    $"StackTrace:\n{root.StackTrace}");
                return;
            }

            // Work out where we'd normally send the user based on role.
            var user = context.User;
            string target = "/Account/Login";
            if (user?.Identity?.IsAuthenticated == true)
            {
                if (user.IsInRole("MainAdmin") || user.IsInRole("Admin")) target = "/Admin/Dashboard";
                else if (user.IsInRole("Teacher")) target = "/Teacher/Dashboard";
                else if (user.IsInRole("Student")) target = "/Student/Dashboard";
            }

            // Loop guard: don't redirect back to the exact page that just failed.
            if (string.Equals(failedPath.TrimEnd('/'), target.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
            {
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(
                    "<h2>Something went wrong loading your dashboard.</h2>" +
                    "<p>Please try again shortly, or <a href='/Account/Logout'>log out</a> and back in.</p>");
                return;
            }

            context.Response.Redirect(target);
        });
    });

    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, private";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
    }

    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; font-src 'self' https://cdnjs.cloudflare.com; img-src 'self' data: https:; connect-src 'self'";

    await next();
});

// Populate common data (unread notifications) for views
app.UseMiddleware<BibekSchool.Middlewares.PopulateCommonDataMiddleware>();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapRazorPages();

app.Run();

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