using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Application;
using TaskManager.Infrastructure;
using Serilog;

// ── SERILOG BOOTSTRAP LOGGER ───────────────────────────
// This catches any errors that happen BEFORE the app fully starts
// (e.g. config errors, DI failures during startup)
// Without this, startup crashes would be invisible
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();
// 

var builder = WebApplication.CreateBuilder(args);


// ── SERILOG AS THE MAIN LOGGER ─────────────────────────
// Replaces the default .NET logger with Serilog
// ctx gives access to configuration and services
// services gives access to the DI container for context enrichment
builder.Host.UseSerilog((ctx, services, config) =>
{
    config
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day);
});
// ─────────────────────────────────────────────────────



// ── APPLICATION SERVICES ──────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
// ─────────────────────────────────────────────────────

// ── JWT AUTHENTICATION ────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey))
    };
});
// ─────────────────────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── OPENAPI — built into .NET 10 ─────────────────────
builder.Services.AddOpenApi();
// ─────────────────────────────────────────────────────

var app = builder.Build();

// ── MIDDLEWARE PIPELINE ───────────────────────────────
app.UseMiddleware<TaskManager.API.Middleware.ExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
// ─────────────────────────────────────────────────────

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    await TaskManager.Infrastructure.Persistence.DbSeeder
        .SeedRolesAsync(scope.ServiceProvider);
}

app.Run();