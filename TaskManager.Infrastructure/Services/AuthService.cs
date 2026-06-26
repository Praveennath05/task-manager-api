using Microsoft.AspNetCore.Identity;
using TaskManager.Domain.Common;
using TaskManager.Domain.Interfaces;


namespace TaskManager.Infrastructure.Services;

public class AuthService : IAuthService
{
    // ── DEPENDENCY INJECTION ──────────────────────────────
    // UserManager — handles user creation, password hashing, roles
    // SignInManager — handles login verification
    // TokenService — generates JWT token after successful login
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly TokenService _tokenService;
    // ─────────────────────────────────────────────────────

    public AuthService(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        TokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    public async Task<Result<string>> RegisterAsync(string email, string password, CancellationToken cancellationToken)
    {
        // ── CHECK DUPLICATE ───────────────────────────────
        // Reject if email already exists in database
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
            return Result<string>.Failure("Email already registered");
        // ─────────────────────────────────────────────────

        // ── CREATE USER ───────────────────────────────────
        // Identity hashes the password automatically — never stored plain
        var user = new IdentityUser
        {
            Email = email,
            UserName = email
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return Result<string>.Failure(
                // Show ALL errors not just first one
                string.Join(", ", result.Errors.Select(e => e.Description)));
        // ─────────────────────────────────────────────────

        // ── ASSIGN DEFAULT ROLE ───────────────────────────
        // Every new user gets "User" role by default
        // Admin role is assigned manually or by another admin
        await _userManager.AddToRoleAsync(user, "User");
        // ─────────────────────────────────────────────────

        return Result<string>.Success("Registration successful");
    }

    public async Task<Result<string>> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        // ── FIND USER ─────────────────────────────────────
        // Look up user by email in the database
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return Result<string>.Failure("Invalid email or password");
        // ─────────────────────────────────────────────────

        // ── VERIFY PASSWORD ───────────────────────────────
        // SignInManager checks the hashed password
        // lockoutOnFailure: true — locks account after 5 failed attempts
        var result = await _signInManager.CheckPasswordSignInAsync(
            user, password, lockoutOnFailure: true);
        if (!result.Succeeded)
            return Result<string>.Failure("Invalid email or password");
        // ─────────────────────────────────────────────────

        // ── GENERATE TOKEN ────────────────────────────────
        // Get user roles — needed inside the JWT claims
        // Then generate the token and return it to the client
        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GenerateToken(user, roles);
        return Result<string>.Success(token);
        // ─────────────────────────────────────────────────
    }
}