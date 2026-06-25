using MediatR;
using Microsoft.AspNetCore.Identity;
using TaskManager.Domain.Common;

namespace TaskManager.Application.Features.Auth.Commands;

public record RegisterCommand(
    string Email,
    string Password,
    string FullName
) : IRequest<Result<string>>;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<string>>
{
    private readonly UserManager<IdentityUser> _userManager;

    public RegisterCommandHandler(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<string>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            return Result<string>.Failure("Email already registered");

        var user = new IdentityUser
        {
            Email = request.Email,
            UserName = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return Result<string>.Failure(result.Errors.First().Description);

        await _userManager.AddToRoleAsync(user, "User");
        return Result<string>.Success("Registration successful");
    }
}