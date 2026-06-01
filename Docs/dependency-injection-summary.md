# Dependency Injection in .NET — Complete Summary

## What Problem Does DI Solve?

Without DI, classes create their own dependencies using `new`:

```csharp
public class CreateTaskCommandHandler
{
    public async Task Handle(CreateTaskCommand cmd)
    {
        var repo = new TaskRepository(); // hardcoded dependency
        await repo.AddAsync(task);
    }
}
```

This causes three problems:

- **Untestable** — `TaskRepository` connects to a real database; you cannot swap it out in tests
- **Tightly coupled** — changing the implementation means hunting down every `new` call
- **No lifecycle control** — every call creates a new object, wasting resources

---

## The Core Idea

> Don't create your dependencies. Declare what you need, and let the outside world provide it.

```csharp
public class CreateTaskCommandHandler
{
    private readonly ITaskRepository _repo;

    // Declare the dependency — don't create it
    public CreateTaskCommandHandler(ITaskRepository repo)
    {
        _repo = repo;
    }
}
```

The key detail: depend on the **interface** (`ITaskRepository`), not the concrete class (`TaskRepository`). This is what makes the dependency swappable.

---

## What the DI Container Does

Manually wiring up a deep dependency chain is painful:

```
A depends on B
B depends on C
C depends on D
D depends on E
```

The DI container solves this. You register the interface-to-implementation mappings once, and the container resolves the entire chain automatically.

```csharp
// Program.cs — register once, resolve everywhere
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IJwtService, JwtService>();
```

From this point on, any class that declares `ITaskRepository` in its constructor receives a `TaskRepository` instance automatically — no manual `new` required.

---

## Service Lifetimes

| Lifetime | Behavior | Use For |
|---|---|---|
| `Scoped` | One instance per HTTP request | Repositories, Handlers, business services |
| `Transient` | New instance every time it is injected | Lightweight, stateless utilities |
| `Singleton` | One instance for the entire application lifetime | Configuration, caches, global state |

> **Common mistake**: injecting a `Scoped` service into a `Singleton`. The Singleton lives forever, but it holds a reference to a `Scoped` object that should have been disposed — this causes data corruption and hard-to-find bugs.

---

## The Full Request Pipeline

Using **GET /api/tasks?projectId=xxx** as a real example:

### Layer 1 — Controller (`TaskHub.Api`)

```csharp
[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;

    // DI injection point 1: IMediator is provided by the container
    public TasksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetTasksByProject([FromQuery] Guid projectId)
    {
        var query = new GetTasksByProjectQuery(projectId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
```

The Controller only receives the request and returns the result. It contains zero business logic.

---

### Layer 2 — Query (`TaskHub.Application`)

```csharp
// A pure data carrier — describes "what to fetch"
public record GetTasksByProjectQuery(Guid ProjectId)
    : IRequest<List<TaskSummaryDto>>;
```

No logic. Just a strongly typed description of intent.

---

### Layer 3 — QueryHandler (`TaskHub.Application`)

```csharp
public class GetTasksByProjectQueryHandler
    : IRequestHandler<GetTasksByProjectQuery, List<TaskSummaryDto>>
{
    private readonly ITaskRepository _repo;

    // DI injection point 2: ITaskRepository is provided by the container
    public GetTasksByProjectQueryHandler(ITaskRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<TaskSummaryDto>> Handle(
        GetTasksByProjectQuery query,
        CancellationToken ct)
    {
        var tasks = await _repo.GetByProjectIdAsync(query.ProjectId);

        // Map Entity → DTO (never expose database objects directly)
        return tasks.Select(t => new TaskSummaryDto
        {
            Id = t.Id,
            Title = t.Title,
            Status = t.Status.ToString(),
            Priority = t.Priority.ToString()
        }).ToList();
    }
}
```

The Handler knows nothing about databases — it only talks to the interface.

---

### Layer 4 — Interface (`TaskHub.Domain`)

```csharp
// Defines what operations are needed — not how they work
public interface ITaskRepository
{
    Task<List<TaskItem>> GetByProjectIdAsync(Guid projectId);
    Task<TaskItem?> GetByIdAsync(Guid id);
    Task AddAsync(TaskItem task);
    Task UpdateAsync(TaskItem task);
    Task DeleteAsync(Guid id);
}
```

This lives in Domain — the innermost layer — with zero dependencies on any framework.

---

### Layer 5 — Implementation (`TaskHub.Infrastructure`)

```csharp
public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _db;

    // DI injection point 3: AppDbContext is provided by the container
    public TaskRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<TaskItem>> GetByProjectIdAsync(Guid projectId)
    {
        return await _db.Tasks
            .Where(t => t.ProjectId == projectId)
            .ToListAsync();
    }
}
```

This is the only layer that knows about EF Core and SQL Server.

---

### Layer 6 — Registration (`Program.cs`)

```csharp
// Register MediatR — auto-scans all Handlers
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(GetTasksByProjectQueryHandler).Assembly));

// Register EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Register repositories — the container uses these mappings to resolve dependencies
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
```

---

## End-to-End Flow

```
1. Frontend sends GET /api/tasks?projectId=abc

2. Container creates TasksController
   → needs IMediator → injects MediatR

3. Controller sends GetTasksByProjectQuery to MediatR

4. MediatR locates the matching Handler
   → Container creates GetTasksByProjectQueryHandler
   → needs ITaskRepository → looks up registration → creates TaskRepository
   → TaskRepository needs AppDbContext → injects it automatically

5. Handler.Handle() executes
   → calls _repo.GetByProjectIdAsync(projectId)
   → TaskRepository queries SQL Server via EF Core
   → returns List<TaskItem>

6. Handler maps List<TaskItem> → List<TaskSummaryDto>
   → returns to MediatR → returns to Controller

7. Controller returns 200 OK + JSON to the frontend
```

---

## DI Injection Points Summary

| Injection Point | Who Needs It | What Is Injected | Why |
|---|---|---|---|
| `TasksController` | `IMediator` | MediatR implementation | Delegates work without knowing the Handler |
| `GetTasksByProjectQueryHandler` | `ITaskRepository` | `TaskRepository` | No knowledge of database details |
| `TaskRepository` | `AppDbContext` | EF Core DbContext | No manual connection management |

---

## Why Interface-Based Injection Matters for Testing

```csharp
// A fake repository for testing — no database needed
public class FakeTaskRepository : ITaskRepository
{
    private readonly List<TaskItem> _tasks = new();

    public async Task<List<TaskItem>> GetByProjectIdAsync(Guid projectId)
        => _tasks.Where(t => t.ProjectId == projectId).ToList();

    public async Task AddAsync(TaskItem task)
        => _tasks.Add(task);
}

// Unit test — fast, isolated, no infrastructure required
var fakeRepo = new FakeTaskRepository();
var handler = new GetTasksByProjectQueryHandler(fakeRepo);
var result = await handler.Handle(new GetTasksByProjectQuery(someProjectId), default);
Assert.NotEmpty(result);
```

In production, the container provides `TaskRepository`.
In tests, you provide `FakeTaskRepository`.
The Handler never knows the difference — it only sees `ITaskRepository`.

---

## One-Line Summary

> Dependency Injection means your classes **declare what they need** rather than **creating what they need** — a container reads those declarations and wires everything together automatically, making your code testable, flexible, and maintainable.
