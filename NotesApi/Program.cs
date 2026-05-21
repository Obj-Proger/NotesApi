using DotNetEnv;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NotesApi.Data;
using NotesApi.Services;
using NotesApi.Validators;
using System.Text;

/// <summary>
/// Application startup and dependency injection configuration.
/// Configures authentication, database, validation, and middleware pipeline.
/// </summary>

// Load environment variables from .env file before creating WebApplicationBuilder.
Env.Load();

// Configure application URLs from environment variables.
// Defaults to localhost ports if not specified.
var httpPort = Environment.GetEnvironmentVariable("APP_HTTP_PORT") ?? "5001";
var httpsPort = Environment.GetEnvironmentVariable("APP_HTTPS_PORT") ?? "7001";
var urls = $"http://0.0.0.0:{httpPort};https://0.0.0.0:{httpsPort}";
Environment.SetEnvironmentVariable("ASPNETCORE_URLS", urls);

var builder = WebApplication.CreateBuilder(args);

// Configure logging to output to console and debug output.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Build PostgreSQL connection string from environment variables with validation.
var connectionString = GetConnectionString();

// Register Entity Framework Core DbContext with PostgreSQL provider.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure JWT bearer authentication for token-based security.
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? throw new InvalidOperationException("JWT_SECRET environment variable is not set.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // RequireHttpsMetadata should be true in production to enforce secure token transmission.
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;

    // Configure token validation parameters for JWT verification.
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = "NotesApi",
        ValidateAudience = true,
        ValidAudience = "NotesApiUsers",
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Configure CORS policy to allow cross-origin requests.
// Warning: "AllowAll" policy is suitable for development only.
// Configure specific origins in production for security.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register application services with scoped lifetime.
// Scoped ensures new instance per HTTP request for proper isolation.
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<ITaskService, TaskService>();

// Register FluentValidation validators from assembly.
// Automatically discovers and registers all validators implementing AbstractValidator<T>.
builder.Services.AddValidatorsFromAssemblyContaining<CreateNoteDtoValidator>();

// Register API explorer and Swagger documentation generator.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register controllers (FluentValidation validators are called automatically via DI).
builder.Services.AddControllers();

var app = builder.Build();

// Execute database migrations automatically on startup.
// Warning: In production, consider running migrations separately via CLI for safety.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    Console.WriteLine("Database migrated successfully");
}

// Configure middleware pipeline in correct order for proper request processing.

// Enable Swagger and Swagger UI in development environment only.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirect HTTP requests to HTTPS in non-development environments.
app.UseHttpsRedirection();

// Enable CORS policy for cross-origin requests.
app.UseCors("AllowAll");

// Authenticate JWT tokens and populate User.Identity.
// Must be before UseAuthorization.
app.UseAuthentication();

// Authorize based on [Authorize] attributes and policies.
// Must be after UseAuthentication.
app.UseAuthorization();

// Map controller routes to endpoints.
app.MapControllers();

Console.WriteLine("Application starting...");
app.Run();

/// <summary>
/// Builds PostgreSQL connection string from environment variables.
/// Validates that all required database configuration variables are set.
/// </summary>
/// <returns>A properly formatted PostgreSQL connection string.</returns>
/// <exception cref="InvalidOperationException">
/// Thrown when any required environment variable (DB_HOST, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD) is not set.
/// </exception>
string GetConnectionString()
{
    // Retrieve all required database configuration from environment variables.
    var host = Environment.GetEnvironmentVariable("DB_HOST");
    var port = Environment.GetEnvironmentVariable("DB_PORT");
    var database = Environment.GetEnvironmentVariable("DB_NAME");
    var username = Environment.GetEnvironmentVariable("DB_USER");
    var password = Environment.GetEnvironmentVariable("DB_PASSWORD");

    // Validate that all required variables are present before attempting connection.
    if (string.IsNullOrWhiteSpace(host))
        throw new InvalidOperationException("DB_HOST is not set.");
    if (string.IsNullOrWhiteSpace(port))
        throw new InvalidOperationException("DB_PORT is not set.");
    if (string.IsNullOrWhiteSpace(database))
        throw new InvalidOperationException("DB_NAME is not set.");
    if (string.IsNullOrWhiteSpace(username))
        throw new InvalidOperationException("DB_USER is not set.");
    if (string.IsNullOrWhiteSpace(password))
        throw new InvalidOperationException("DB_PASSWORD is not set.");

    // Build and return the connection string with all components.
    return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
}