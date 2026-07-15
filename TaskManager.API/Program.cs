using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Application;
using TaskManager.Infrastructure;
using Serilog;
using Hangfire;
using Microsoft.AspNetCore.RateLimiting;


// ── SERILOG BOOTSTRAP LOGGER ───────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();
 

var builder = WebApplication.CreateBuilder(args);


// ── SERILOG AS THE MAIN LOGGER ─────────────────────────
builder.Host.UseSerilog((ctx, services, config) =>
{
    config
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day);
});

// ── APPLICATION SERVICES ──────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);


// ── HANGFIRE ────────────────────────────────────────────
builder.Services.AddHangfire(config =>
{
    config.UseSqlServerStorage(
        "Server=ASUS;Database=TaskManagerDb;Trusted_Connection=True;TrustServerCertificate=True");
});

// ── HANGFIRE SERVER ──────────────────────────────────────
builder.Services.AddHangfireServer();


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
})
.AddCookie("External") // temporary holding scheme for the Google login result
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    options.CallbackPath = "/signin-google";
    options.SignInScheme = "External";
});


builder.Services.AddControllers();
// ── RATE LIMITING ──────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    // ── LOGIN/REGISTER POLICY ────────────────────────
    // Max 5 attempts per minute, per client IP
    // After that, requests get rejected with 429 Too Many Requests
    options.AddFixedWindowLimiter("AuthPolicy",limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});
builder.Services.AddEndpointsApiExplorer();

// ── NSWAG — OpenAPI + Swagger UI ──────────────────────
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "TaskManager API";
    config.Version = "v1";

    // ── JWT SUPPORT IN SWAGGER UI ─────────────────────
    config.AddSecurity("JWT", Enumerable.Empty<string>(),
        new NSwag.OpenApiSecurityScheme
        {
            Type = NSwag.OpenApiSecuritySchemeType.ApiKey,
            Name = "Authorization",
            In = NSwag.OpenApiSecurityApiKeyLocation.Header,
            Description = "Enter: Bearer {your token}"
        });

    config.OperationProcessors.Add(
        new NSwag.Generation.Processors.Security.AspNetCoreOperationSecurityScopeProcessor("JWT"));
});



var app = builder.Build();

// ── MIDDLEWARE PIPELINE ───────────────────────────────
app.UseMiddleware<TaskManager.API.Middleware.ExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment())
{
    // ── SWAGGER UI ──────────────────────────────────
    app.UseOpenApi();
    app.UseSwaggerUi();
    
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.UseMiddleware<TaskManager.API.Middleware.BlacklistCheckMiddleware>();
// ── HANGFIRE DASHBOARD ───────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    await TaskManager.Infrastructure.Persistence.DbSeeder
        .SeedRolesAsync(scope.ServiceProvider);
}
RecurringJob.AddOrUpdate<TaskManager.Domain.Interfaces.ITaskReminderJob>(
    "overdue-task-check",
    job => job.CheckOverdueTasksAsync(),
    Cron.Daily
);


app.Run();