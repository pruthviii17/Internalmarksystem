using InternalMarksSystem.Data;
using InternalMarksSystem.Models;
using InternalMarksSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;

if (args.Contains("--test-engine"))
{
    InternalMarksSystem.VerificationRunner.RunTests();
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// Configure Database Provider (Pomelo MySQL with smart local fallback)
string providerConfig = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "Auto";
string mySqlConnStr = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=localhost;Port=3306;Database=internal_marks_db;User=root;Password=;CharSet=utf8mb4;";
string sqliteConnStr = builder.Configuration.GetConnectionString("SqliteConnection") 
    ?? "Data Source=internal_marks.db";

bool useMySql = false;
if (string.Equals(providerConfig, "MySql", StringComparison.OrdinalIgnoreCase))
{
    useMySql = true;
}
else if (string.Equals(providerConfig, "Sqlite", StringComparison.OrdinalIgnoreCase))
{
    useMySql = false;
}
else // "Auto"
{
    try
    {
        using var client = new TcpClient();
        var connectTask = client.ConnectAsync("127.0.0.1", 3306);
        if (connectTask.Wait(TimeSpan.FromMilliseconds(500)) && client.Connected)
        {
            useMySql = true;
        }
    }
    catch
    {
        useMySql = false;
    }
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (useMySql)
    {
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
        options.UseMySql(mySqlConnStr, serverVersion, mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        });
    }
    else
    {
        options.UseSqlite(sqliteConnStr);
    }
});

// Configure ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password requirements for college project convenience
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;

    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure Cookie Authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

// Register Core Domain Services
builder.Services.AddScoped<IMarksCalculationEngine, MarksCalculationEngine>();
builder.Services.AddScoped<IAuditService, AuditService>();

var app = builder.Build();

// Seed Database and apply schema
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var calcEngine = services.GetRequiredService<IMarksCalculationEngine>();

        logger.LogInformation("Initializing database with provider: {Provider}", useMySql ? "Pomelo MySQL" : "SQLite");
        await DbInitializer.InitializeAsync(context, userManager, roleManager, calcEngine);
        logger.LogInformation("Database initialization and seeding completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
