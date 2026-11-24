using System.Text;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using SmartStock.API.Middleware;
using SmartStock.Core.Common;
using SmartStock.Core.Interfaces;
using SmartStock.Core.Validation;
using SmartStock.Infrastructure.AI;
using SmartStock.Infrastructure.Data;
using SmartStock.Infrastructure.Data.Seed;
using SmartStock.Infrastructure.Forecasting;
using SmartStock.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Configure Server Limits & Request Timeouts
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
});

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100MB
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// Database Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? builder.Configuration["ConnectionStrings__DefaultConnection"]
                       ?? "Host=localhost;Port=5432;Database=smartstock_db;Username=postgres;Password=Admin@123;Include Error Detail=true";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(180);
    });
});

builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] 
                ?? builder.Configuration["Jwt__Secret"] 
                ?? "SmartStockSuperSecureJwtSigningKeyForHackathon2026WithSufficientLength!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] 
                ?? builder.Configuration["Jwt__Issuer"] 
                ?? "SmartStockAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] 
                  ?? builder.Configuration["Jwt__Audience"] 
                  ?? "SmartStockClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Register HTTP Context & Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBusinessService, BusinessService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IPurchaseService, PurchaseService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IForecastingService, ForecastingService>();
builder.Services.AddScoped<ISupplierInvoiceService, SupplierInvoiceService>();

// Customer & Recurring Order Management Services
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ICustomerRecurringProductService, CustomerRecurringProductService>();
builder.Services.AddScoped<ICustomerRecurringSettingService, CustomerRecurringSettingService>();
builder.Services.AddScoped<IRecurringOrderService, RecurringOrderService>();
builder.Services.AddScoped<ICustomerPortalService, CustomerPortalService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IRecurringOrderSchedulerService, RecurringOrderSchedulerService>();

builder.Services.AddHttpClient<IAIEmailGeneratorService, AIEmailGeneratorService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(3);
});

builder.Services.AddHttpClient<IAIEmailReplyProcessor, AIEmailReplyProcessor>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(3);
});

builder.Services.AddHostedService<RecurringOrderBackgroundService>();

builder.Services.AddHttpClient<IAIAssistantService, AIAssistantService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(3);
});

builder.Services.AddHttpClient<IInvoiceScannerService, InvoiceScannerService>(client =>
{
    // Multi-page invoices and Gemini multimodal extraction can take up to several minutes.
    client.Timeout = TimeSpan.FromMinutes(5);
});

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// CORS configuration
var allowedOriginsConfig = builder.Configuration["Frontend:Origin"] 
                           ?? builder.Configuration["Frontend__Origin"] 
                           ?? "http://localhost:5173;http://localhost:3000;http://localhost:8080";

var allowedOrigins = allowedOriginsConfig
    .Split([';', ','], StringSplitOptions.RemoveEmptyEntries)
    .Select(o => o.Trim().TrimEnd('/'))
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Controllers & Custom Model Validation formatting
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors.Select(err => new ValidationError(
                    field: JsonNamingPolicy.CamelCase.ConvertName(e.Key),
                    message: string.IsNullOrWhiteSpace(err.ErrorMessage) ? "Invalid input value" : err.ErrorMessage
                )))
                .ToList();

            var response = ApiResponse<object>.Fail("Validation failed.", errors);
            return new BadRequestObjectResult(response);
        };
    });

// Swagger / OpenAPI documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SmartStock API",
        Version = "v1",
        Description = "AI-Powered Inventory & Retail Business Management API for Indian Kirana & Small Retailers",
        Contact = new OpenApiContact
        {
            Name = "SmartStock Engineering Team",
            Email = "support@smartstock.in"
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Format: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure Middleware Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartStock API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowFrontend");

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health Check / Ping Endpoint
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "SmartStock Backend API",
    version = "1.0.0",
    timestamp = DateTime.UtcNow
}));

// Apply Migrations and Seed on Startup
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    Log.Information("Applying PostgreSQL database migrations...");
    await dbContext.Database.MigrateAsync();
    Log.Information("Seeding database demo data...");
    await DatabaseSeeder.SeedAsync(dbContext);
    Log.Information("Database migration and seed completed successfully.");
}
catch (Exception ex)
{
    Log.Error(ex, "An error occurred while migrating or seeding the database.");
}

app.Run();

// For integration testing WebApplicationFactory reference
public partial class Program { }
