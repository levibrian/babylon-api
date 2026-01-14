using Babylon.Alfred.Api.Features.Startup.Extensions;
using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Middlewares;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Converters;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

try
{
    Log.Information("Starting Babylon.Alfred.Api application");

    // Add services to the container.
    builder.Services
    .AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.Converters.Add(new StringEnumConverter());
        options.SerializerSettings.Converters.Add(new UnixDateTimeConverter()); // Ensure UnixDateTimeConverter is used
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000" }; // Default to localhost:3000 for development

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Configure Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<BabylonDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Enable retry on failure for transient errors
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    }));

// Configure services
builder.Services.RegisterFeatures();

// Configure JWT Authentication
var secretKey = builder.Configuration["Authentication:Jwt:SecretKey"];
if (!string.IsNullOrEmpty(secretKey))
{
    var key = System.Text.Encoding.UTF8.GetBytes(secretKey);
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // For dev; true for prod
        options.SaveToken = true;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Authentication:Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Authentication:Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
// Request logging should come first to capture all requests
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<GlobalErrorHandlerMiddleware>();

// CORS must be before UseAuthorization and MapControllers
app.UseCors();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Babylon API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthorization();

app.MapControllers();

Log.Information("Babylon.Alfred.Api application started successfully");

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
