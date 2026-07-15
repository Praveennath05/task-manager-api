using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TaskManager.Domain.Interfaces;

namespace TaskManager.API.Middleware;

public class BlacklistCheckMiddleware
{
    private readonly RequestDelegate _next;

    public BlacklistCheckMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICacheService cache)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var jti = context.User.FindFirstValue(JwtRegisteredClaimNames.Jti);

            Console.WriteLine($"[DEBUG] Checking blacklist for jti: {jti}");

            if (!string.IsNullOrEmpty(jti))
            {
                var isBlacklisted = await cache.GetAsync<bool?>(
                    $"blacklist:{jti}", context.RequestAborted);

                Console.WriteLine($"[DEBUG] isBlacklisted result: {isBlacklisted}");

                if (isBlacklisted == true)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Token has been revoked");
                    return;
                }
            }
        }

        await _next(context);
    }
}