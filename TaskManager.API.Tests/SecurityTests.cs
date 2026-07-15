
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace TaskManager.API.Tests;

// ── IClassFixture ──────────────────────────────────────
// Reuses ONE instance of the API across all tests in this class
// instead of booting it up fresh for every single test —
// much faster, since starting the whole app has real overhead
public class SecurityTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SecurityTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── TEST 1 — TAMPERED TOKEN ─────────────────────────
    // Take a real, valid token and change ONE character
    // in its signature — this should invalidate it entirely
    [Fact]
    public async Task GetTasks_WithTamperedToken_ShouldReturn401()
    {
        // ── ARRANGE ────────────────────────────────────
        // Login to get a real, valid token first
        var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", new
        {
            email = "test@gmail.com",
            password = "Test@1234"
        });

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        var validToken = loginResult!.AccessToken;

        // ── TAMPER WITH THE TOKEN ──────────────────────
        // Flip the last character of the signature — a JWT's
        // signature is the part after the LAST '.' character
        // Any change here breaks cryptographic verification
        var tamperedToken = validToken[..^1] + (validToken[^1] == 'A' ? 'B' : 'A');
        // ─────────────────────────────────────────────────

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tamperedToken);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var response = await _client.GetAsync("/api/Tasks");
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        // ─────────────────────────────────────────────────

        _client.DefaultRequestHeaders.Authorization = null; // cleanup
    }

    // ── TEST 2 — MISSING AUTHORIZATION HEADER ───────────
    [Fact]
    public async Task GetTasks_WithNoAuthHeader_ShouldReturn401()
    {
        // ── ARRANGE ────────────────────────────────────
        _client.DefaultRequestHeaders.Authorization = null;
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var response = await _client.GetAsync("/api/Tasks");
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        // ─────────────────────────────────────────────────
    }

    // ── TEST 3 — MALFORMED TOKEN ─────────────────────────
    // Not even a real JWT structure — just garbage text
    [Fact]
    public async Task GetTasks_WithMalformedToken_ShouldReturn401()
    {
        // ── ARRANGE ────────────────────────────────────
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "this-is-not-a-real-jwt-at-all");
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var response = await _client.GetAsync("/api/Tasks");
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        // ─────────────────────────────────────────────────

        _client.DefaultRequestHeaders.Authorization = null;
    }

    // ── TEST 4 — ROLE BYPASS ATTEMPT ─────────────────────
    // A regular "User" role trying to hit an Admin-only endpoint
    // (Delete task) — should be blocked with 403, not 401
    // (401 = "who are you?", 403 = "I know who you are, but no")
    [Fact]
    public async Task DeleteTask_AsRegularUser_ShouldReturn403()
    {
        // ── ARRANGE ────────────────────────────────────
        var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", new
        {
            email = "test@gmail.com",
            password = "Test@1234"
        });

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        // Attempt to delete ANY task id — doesn't matter if it
        // exists, since role check happens BEFORE the handler runs
        var response = await _client.DeleteAsync("/api/Tasks/1");
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        // ─────────────────────────────────────────────────

        _client.DefaultRequestHeaders.Authorization = null;
    }

    // ── HELPER RECORD ──────────────────────────────────
    // Matches the shape of your /api/Auth/login response
    private record LoginResponse(string AccessToken, string RefreshToken);
}