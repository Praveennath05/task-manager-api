using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TaskManager.Web;
using TaskManager.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ── HTTP CLIENT — POINTS AT THE REAL API ──────────────────
// This is NOT Blazor's own address — it's your separate
// TaskManager.API project, running on its own port
// Every API call from Blazor (login, get tasks, etc.) goes here
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5097")
});
// ✅ Register AuthService
builder.Services.AddScoped<AuthService>();


// ─────────────────────────────────────────────────────────

// ── CORS — ALLOW BLAZOR TO CALL THIS API ──────────────────
// Blazor WASM runs on a different port than the API — browsers
// block cross-origin requests by default unless we explicitly
// allow it here

// ─────────────────────────────────────────────────────────
await builder.Build().RunAsync();