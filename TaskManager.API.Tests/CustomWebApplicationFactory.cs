using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskManager.API.Tests;

// ── CUSTOM FACTORY ─────────────────────────────────────
// Boots up your ENTIRE actual API in-memory for testing —
// real middleware pipeline, real JWT validation, real
// Program.cs — just without a real network port
// Program must be public/internal-visible for this to work
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
}
// ─────────────────────────────────────────────────────
