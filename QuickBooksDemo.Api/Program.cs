using Microsoft.EntityFrameworkCore;
using QuickBooksDemo.DAL.Context;
using QuickBooksDemo.Service.Interfaces;
using QuickBooksDemo.Service.Implementations;
using QuickBooksDemo.Service.Services;
using QuickBooksDemo.Models.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure port for deployment compatibility
var port = Environment.GetEnvironmentVariable("PORT") ?? "5042";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS for Angular
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                  "http://localhost:4200",                     // Local development
                  "https://quickbooksdemo-ui.onrender.com"    // Production frontend
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configure Entity Framework with DATABASE_URL support for Render deployment
var connectionString = builder.Configuration["DATABASE_URL"];

if (!string.IsNullOrEmpty(connectionString))
{
    // Convert PostgreSQL URI format to Npgsql connection string format for Render
    connectionString = ConvertPostgresUriToConnectionString(connectionString);
}
else
{
    // Fall back to appsettings.json connection string (development)
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

Console.WriteLine($"Using PostgreSQL database connection: {!string.IsNullOrEmpty(connectionString)}");

builder.Services.AddDbContext<QuickBooksDemoContext>(options =>
    options.UseNpgsql(connectionString));

// Configure QuickBooks settings
builder.Services.Configure<QuickBooksConfig>(
    builder.Configuration.GetSection("QuickBooks"));

// Register services
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ITechnicianService, TechnicianService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IQuickBooksApiService, QuickBooksApiService>();
builder.Services.AddScoped<IQuickBooksTokenService, QuickBooksTokenService>();
builder.Services.AddScoped<IQuickBooksIntegrationService, QuickBooksIntegrationService>();
builder.Services.AddScoped<IReseedService, ReseedService>();

var app = builder.Build();

// Configure pipeline (ORDER MATTERS!)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();           // Must be before UseAuthorization
app.UseRouting();
app.UseAuthorization();

app.MapControllers();

// Ensure database is migrated and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<QuickBooksDemoContext>();
    context.Database.Migrate();
}

app.Run();

/// <summary>
/// Converts PostgreSQL URI format to Npgsql connection string format
/// Example: postgres://user:pass@host:port/db?sslmode=require
/// Becomes: Host=host;Port=port;Database=db;Username=user;Password=pass;SSL Mode=Require;
/// </summary>
static string ConvertPostgresUriToConnectionString(string databaseUrl)
{
    try
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        var username = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : "";

        var connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.LocalPath.TrimStart('/')};Username={username};Password={password};";

        // Parse query parameters
        if (!string.IsNullOrEmpty(uri.Query))
        {
            var queryParams = uri.Query.TrimStart('?').Split('&');
            foreach (var param in queryParams)
            {
                var keyValue = param.Split('=');
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].ToLower();
                    var value = keyValue[1];

                    if (key == "sslmode")
                    {
                        connectionString += $"SSL Mode={value.Replace("require", "Require")};";
                    }
                    else if (key == "channel_binding")
                    {
                        // Skip channel_binding as it's not supported by Npgsql connection string format
                    }
                    else
                    {
                        connectionString += $"{key}={value};";
                    }
                }
            }
        }

        return connectionString;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error converting DATABASE_URL: {ex.Message}");
        return databaseUrl; // Return original if conversion fails
    }
}
