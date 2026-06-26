# TaskManager — Project State
_Last updated: 26 June 2026_

---

## Stack
- .NET 10 / ASP.NET Core Web API
- Entity Framework Core 10
- SQL Server (Server=ASUS, DB=TaskManagerDb)
- MediatR + CQRS
- ASP.NET Core Identity + JWT Bearer
- Swashbuckle (Swagger)
- Clean Architecture (4 layers)

---

## What's Built & Working ✅

### Layer 1 — Domain
- `BaseEntity.cs` — Id, CreatedAt, UpdatedAt
- `WorkTask.cs` — Title, Description, IsCompleted, DueDate
- `IWorkTaskRepository.cs` — 5 methods, all with CancellationToken
- `IAuthService.cs` — RegisterAsync, LoginAsync (both with CancellationToken)
- `Result<T>.cs` — in Domain.Common (moved from Application)

### Layer 2 — Application
- `DependencyInjection.cs` — registers MediatR
- `Features/Tasks/Commands/` — Create, Update, Delete
- `Features/Tasks/Queries/` — GetAll, GetById
- `Features/Auth/Commands/` — RegisterCommand, LoginCommand
- All handlers use IAuthService / IWorkTaskRepository (no Identity/EF in Application)
- CancellationToken passed through all handlers and repository calls

### Layer 3 — Infrastructure
- `AppDbContext.cs` — extends IdentityDbContext<IdentityUser>
- `WorkTaskRepository.cs` — implements IWorkTaskRepository, all CancellationToken-aware
- `AuthService.cs` — implements IAuthService (register + login + lockout)
- `TokenService.cs` — generates JWT with claims (userId, email, role, jti)
- `DbSeeder.cs` — seeds Admin and User roles at startup
- `DependencyInjection.cs` — registers EF Core, Identity, repos, AuthService, TokenService
- `AppDbContextFactory.cs` — hardcoded connection string for EF migrations

### Layer 4 — API
- `TasksController.cs` — full CRUD, [Authorize] on class, [Authorize(Roles="Admin")] on Delete
- `AuthController.cs` — POST /api/Auth/register, POST /api/Auth/login
- `ExceptionHandlingMiddleware.cs` — global error handler
- `Program.cs` — JWT middleware, Identity, Swagger, DbSeeder call, correct middleware order

### Database
- Migrations applied: InitialCreate + AddIdentity
- Tables: Tasks, AspNetUsers, AspNetRoles, AspNetUserRoles, __EFMigrationsHistory
- Roles seeded: Admin, User

---

## Tested & Verified ✅
- POST /api/Auth/register → 200 "Registration successful"
- POST /api/Auth/login → 200 + JWT token
- GET /api/Tasks (no token) → 401 Unauthorized
- GET /api/Tasks (with token) → 200 + task list
- DELETE /api/Tasks/2 (User role token) → 403 Forbidden ✅

---

## Known Issues / Notes
- Connection string is hardcoded in `DependencyInjection.cs` and `AppDbContextFactory.cs` (not reading from appsettings.json at migration time — known workaround)
- Swagger Authorize button (padlock) not showing — JWT Swagger config has OpenApi namespace issues with Swashbuckle v10 + OpenApi v2.7.5. Testing done via curl instead.
- appsettings.json has correct JWT settings but runtime reads from hardcoded string

---

## File Structure
```
D:\TaskManager\
├── TaskManager.sln
├── TaskManager.Domain\
│   ├── Common\Result.cs
│   ├── Entities\WorkTask.cs
│   └── Interfaces\IWorkTaskRepository.cs, IAuthService.cs
├── TaskManager.Application\
│   ├── DependencyInjection.cs
│   ├── Common\ (empty — Result moved to Domain)
│   └── Features\
│       ├── Tasks\Commands\ (Create, Update, Delete)
│       ├── Tasks\Queries\ (GetAll, GetById)
│       └── Auth\Commands\ (Register, Login)
├── TaskManager.Infrastructure\
│   ├── DependencyInjection.cs
│   ├── Persistence\AppDbContext.cs, DbSeeder.cs, AppDbContextFactory.cs
│   ├── Repositories\WorkTaskRepository.cs
│   └── Services\AuthService.cs, TokenService.cs
└── TaskManager.API\
    ├── Controllers\TasksController.cs, AuthController.cs
    ├── Middleware\ExceptionHandlingMiddleware.cs
    ├── Program.cs
    └── appsettings.json
```

---

## Next Steps (in order)
1. Fix Swagger Authorize button (padlock) — so JWT can be tested in browser UI
2. Add FluentValidation to all commands (Register, Login, CreateTask, UpdateTask)
3. Fix connection string — read from appsettings.json properly at runtime
4. Add Admin user seed (hardcoded admin account at startup)
5. Add user-scoped tasks — tasks should belong to the logged-in user (UserId on WorkTask)
6. Add Projects module — WorkTask belongs to a Project
7. Add Serilog logging
8. Add Redis caching for GetAll
9. Add Hangfire background jobs (email reminders)
10. Add Blazor frontend (Phase 3, Month 7+)

---

## GitHub
Repo: task-manager-api
Last commit: feat: complete JWT authentication — register, login, secured endpoints

---

## How to Resume
```bash
cd D:\TaskManager
dotnet run --project TaskManager.API
# Swagger: http://localhost:5097/swagger
# Test login via curl:
curl -X POST http://localhost:5097/api/Auth/login -H "Content-Type: application/json" -d '{"email":"test@gmail.com","password":"Test@1234"}'
```
