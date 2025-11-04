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

// Configure Entity Framework
builder.Services.AddDbContext<QuickBooksDemoContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ??
                     "Data Source=quickbooksdemo.db"));

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

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<QuickBooksDemoContext>();
    context.Database.EnsureCreated();
}

app.Run();
