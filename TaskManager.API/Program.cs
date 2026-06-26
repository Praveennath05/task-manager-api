using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Application;
using TaskManager.Infrastructure;
using TaskManager.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── APPLICATION SERVICES ──────────────────────────────
// Registers MediatR, FluentValidation — Application layer
builder.Services.AddApplication();

// Registers EF Core, Identity, Repositories, AuthService — Infrastructure layer
builder.Services.AddInfrastructure(builder.Configuration);
// ─────────────────────────────────────────────────────

// ── JWT AUTHENTICATION ────────────────────────────────
// Tells ASP.NET Core to use JWT Bearer tokens
// Every request with Authorization: Bearer <token> header
// will be validated automatically before hitting the controller
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]!;

builder.Services.AddAuthentication(options =>
{
    // Default scheme — use JWT for everything
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Validate the server that created the token
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],

        // Validate the recipient of the token
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],

        // Validate the token expiry
        ValidateLifetime = true,

        // Validate the secret key signature
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey))
    };
});
// ─────────────────────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

// ── BUILD APP ─────────────────────────────────────────
// Must call Build() before using app.Use... methods
var app = builder.Build();
// ─────────────────────────────────────────────────────
// ── MIDDLEWARE PIPELINE ──────────────────────────────

// Order matters — each middleware runs in this exact sequence
app.UseMiddleware<TaskManager.API.Middleware.ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Authentication must come before Authorization
// Auth = who are you? Authz = what can you do?
app.UseAuthentication();
app.UseAuthorization();
// ─────────────────────────────────────────────────────

app.MapControllers();
using (var scope = app.Services.CreateScope())
{
    await TaskManager .Infrastructure.Persistence.DbSeeder
    .SeedRolesAsync(scope.ServiceProvider);
}
app.Run();