using System.Net.Http.Json;
using TaskManager.Web.Models;

namespace TaskManager.Web.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AuthResponse?> LoginAsync(string email, string password)
    {
        var request = new LoginRequest { Email = email, Password = password };

        var response = await _httpClient.PostAsJsonAsync("/api/Auth/login", request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<AuthResponse>();
    }
}