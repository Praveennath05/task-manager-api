# Task Manager API

A professional Task Manager REST API built with **Clean Architecture**, **CQRS**, **MediatR**, **Entity Framework Core**, and **ASP.NET Core (.NET 10)**.

This project follows industry-standard patterns used in enterprise .NET applications and serves as a portfolio project demonstrating real-world backend development skills.

---

## Tech Stack

| Tool | Purpose |
|------|---------|
| ASP.NET Core (.NET 10) | Web API framework |
| Entity Framework Core | ORM — database access |
| MediatR | CQRS pattern implementation |
| FluentValidation | Input validation |
| SQL Server Express | Local database |
| Swagger / Swashbuckle | API documentation and testing |

---

## Architecture

This project follows **Clean Architecture** with 4 layers:

```
TaskManager/
├── TaskManager.Domain          # Business rules and entities
├── TaskManager.Application     # Features, commands, queries (CQRS)
├── TaskManager.Infrastructure  # Database, repositories (EF Core)
└── TaskManager.API             # Controllers, middleware, entry point
```

### Layer responsibilities

- **Domain** — Core entities and repository interfaces. No dependencies on any framework.
- **Application** — All features built as Commands (write) and Queries (read) using MediatR. Business logic lives here.
- **Infrastructure** — EF Core DbContext, SQL Server connection, repository implementations.
- **API** — ASP.NET Core controllers. Receives HTTP requests and passes them to MediatR.

---

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/Tasks | Get all tasks |
| GET | /api/Tasks/{id} | Get task by ID |
| POST | /api/Tasks | Create a new task |
| PUT | /api/Tasks/{id} | Update a task |
| DELETE | /api/Tasks/{id} | Delete a task |

---

## Getting Started

### Prerequisites
- .NET 10 SDK
- SQL Server Express
- VS Code with C# Dev Kit extension

### Setup

1. Clone the repository
```bash
git clone https://github.com/yourusername/task-manager-api.git
cd task-manager-api
```

2. Update the connection string in `TaskManager.Infrastructure/DependencyInjection.cs` with your SQL Server instance name.

3. Run database migrations
```bash
dotnet ef database update --project TaskManager.Infrastructure --startup-project TaskManager.API
```

4. Run the API
```bash
dotnet run --project TaskManager.API
```

5. Open Swagger UI
```
http://localhost:5097/swagger
```

---

## Project Patterns Used

- **Clean Architecture** — separation of concerns across 4 layers
- **CQRS** — Commands for write operations, Queries for read operations
- **MediatR** — decouples request senders from handlers
- **Repository Pattern** — abstracts database access from business logic
- **Dependency Injection** — built-in .NET DI container
- **Global Exception Handling** — middleware catches all unhandled errors

---

## Author

Praveennath — .NET Developer (learning)
