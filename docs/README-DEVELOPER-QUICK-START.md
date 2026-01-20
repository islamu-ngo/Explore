# ISLAMU Event

<div align="center">

**Open-Source Event Platform for Developers**

[![Build](https://img.shields.io/github/actions/workflow/status/islamu-ngo/Explore/build.yml?branch=main&style=flat-square)](https://github.com/islamu-ngo/Explore/actions)
[![Coverage](https://img.shields.io/codecov/c/github/islamu-ngo/Explore?style=flat-square)](https://app.codecov.io/github/islamu-ngo/Explore)
[![License: AGPL-3.0](https://img.shields.io/github/license/islamu-ngo/Explore?style=flat-square)](LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat-square)](docs/CONTRIBUTING.md)
[![Discord](https://img.shields.io/discord/1357505436479131668?color=%237289da&label=Discord&style=flat-square)](https://discord.gg/wrkY824Yv5)

**Clean Architecture • CQRS • ATProto Federation**

[Quick Start](#-5-minute-quick-start) • [API Docs](#-api-reference) • [Architecture](#-architecture) • [Contributing](#-contributing)

</div>

---

## 🚀 5-Minute Quick Start

### Option 1: Docker (Recommended)

```bash
# Clone and run
git clone https://github.com/islamu-ngo/Explore.git
cd Explore
docker-compose up -d

# API: http://localhost:7001
# Blazor: http://localhost:7002
# Swagger: http://localhost:7001/swagger
# Scalar: http://localhost:7001/scalar/v1
```

### Option 2: Local (.NET)

```bash
# Prerequisites: .NET 10 SDK + PostgreSQL 17
git clone https://github.com/islamu-ngo/Explore.git
cd Explore
dotnet restore

# Update connection string in Explore.API/appsettings.Development.json
dotnet ef database update --project Explore.Persistence

# Run with Aspire (recommended)
dotnet run --project Explore.AppHost
# Aspire Dashboard: https://localhost:17225

# Or run projects separately
dotnet run --project Explore.API &
dotnet run --project Explore.Blazor &
```

**That's it!** 🎉 You now have a fully functional event platform running locally.

---

## 📁 Project Structure

```
Explore/
├── Explore.Domain/              # Entities, Value Objects (zero dependencies)
│   ├── Event.cs
│   ├── Organization.cs
│   └── User.cs
│
├── Explore.Application/         # Business logic (CQRS)
│   ├── Features/
│   │   ├── Events/
│   │   │   ├── Requests/
│   │   │   │   ├── Commands/    # CreateEventCommand, UpdateEventCommand
│   │   │   │   └── Queries/     # GetEventListRequest, GetEventDetailsRequest
│   │   │   └── Handlers/
│   │   │       ├── Commands/    # CreateEventCommandHandler
│   │   │       └── Queries/     # GetEventListRequestHandler
│   │   └── Organizations/
│   ├── DTOs/
│   │   └── Event/
│   │       ├── EventDto.cs
│   │       ├── CreateEventDto.cs
│   │       └── Validators/
│   └── Contracts/Persistence/   # Repository interfaces
│
├── Explore.Persistence/         # EF Core + Repositories
│   ├── ExploreDbContext.cs
│   ├── Configurations/          # Entity configurations
│   └── Repositories/            # Repository implementations
│
├── Explore.Infrastructure/      # External services
│   ├── Auth/
│   ├── Storage/
│   └── Email/
│
├── Explore.API/                 # ASP.NET Core Web API
│   └── Controllers/
│       └── EventController.cs
│
├── Explore.Blazor/              # Blazor Server
│   └── Pages/
│       └── Events/
│
└── Explore.Blazor.Client/       # Blazor WASM
```

**Key Directories:**
- **Domain:** Pure C# entities (no frameworks)
- **Application:** MediatR commands/queries/handlers
- **Persistence:** EF Core + PostgreSQL
- **API:** REST endpoints (controllers)
- **Blazor:** Frontend (Server + WASM)

---

## 🛠️ Tech Stack at a Glance

| Component | Technology |
|-----------|-----------|
| **Backend** | .NET 10, ASP.NET Core |
| **CQRS** | MediatR |
| **Validation** | FluentValidation |
| **Mapping** | AutoMapper |
| **ORM** | Entity Framework Core 10 |
| **Database** | PostgreSQL 17 + PostGIS |
| **Frontend** | Blazor (Server + WASM) |
| **UI Library** | MudBlazor |
| **Auth** | Keycloak (OIDC) |
| **API Docs** | Scalar + Swagger |

---

## 🏗️ Architecture

### Clean Architecture Flow

```
HTTP Request → Controller → MediatR → Handler → Repository → Entity → DTO → Response
```

**Dependency Flow (Inward):**
```
API/Blazor → Infrastructure → Application → Domain
```

**Domain has ZERO dependencies** — pure C# business logic.

### CQRS Pattern

**Commands (Write):**
```csharp
// 1. Create command
public class CreateEventCommand : IRequest<BaseCommandResponse<Guid>>
{
    public CreateEventDto EventDto { get; set; }
}

// 2. Create handler
public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken ct)
    {
        // Validate
        var validator = new CreateEventDtoValidator(_organizationRepository);
        var result = await validator.ValidateAsync(request.EventDto, ct);

        // Map and save
        var event = _mapper.Map<Event>(request.EventDto);
        event = await _eventRepository.Create(event);

        return new BaseCommandResponse<Guid> { Success = true, Id = event.Id };
    }
}

// 3. Use in controller
[HttpPost]
public async Task<ActionResult> Create(CreateEventDto dto)
    => Ok(await _mediator.Send(new CreateEventCommand { EventDto = dto }));
```

**Queries (Read):**
```csharp
// 1. Create query
public class GetEventListRequest : IRequest<List<EventListDto>> { }

// 2. Create handler
public class GetEventListRequestHandler : IRequestHandler<GetEventListRequest, List<EventListDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public async Task<List<EventListDto>> Handle(GetEventListRequest request, CancellationToken ct)
    {
        var events = await _eventRepository.GetEventsWithDetails();
        return _mapper.Map<List<EventListDto>>(events);
    }
}

// 3. Use in controller
[HttpGet]
public async Task<ActionResult> GetAll()
    => Ok(await _mediator.Send(new GetEventListRequest()));
```

---

## 📚 API Reference

### Base URL

```
Local: http://localhost:7001/api/v1
Production: https://api.explore.openislamu.org/api/v1
```

### Interactive Documentation

- **Scalar (Modern):** http://localhost:7001/scalar/v1
- **Swagger (OpenAPI 3.0):** http://localhost:7001/swagger

### Quick Examples

#### Get All Events
```bash
curl -X GET "http://localhost:7001/api/v1/Event" \
  -H "accept: application/json"
```

#### Get Event by ID
```bash
curl -X GET "http://localhost:7001/api/v1/Event/{id}" \
  -H "accept: application/json"
```

#### Create Event (Requires Auth)
```bash
curl -X POST "http://localhost:7001/api/v1/Event" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "title": "Islamic Finance Workshop",
    "description": "Introduction to Islamic banking principles",
    "eventTypeId": 1,
    "organizationId": "00000000-0000-0000-0000-000000000000",
    "audienceGenderId": 1,
    "audienceAgeId": 3
  }'
```

### Authentication

**Get Access Token:**
```bash
# Via Keycloak (OIDC)
curl -X POST "http://localhost:8080/realms/islamu/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=explore-api" \
  -d "username=youruser" \
  -d "password=yourpass"

# Use the access_token in Authorization header
curl -H "Authorization: Bearer YOUR_ACCESS_TOKEN" ...
```

**Authorization Levels:**
- `[AllowAnonymous]` — Public read access (GET endpoints)
- `[Authorize]` — Authenticated users (POST/PUT/DELETE)
- `[Authorize(Roles = "Admin")]` — Admin-only operations

---

## 🧪 Testing

### Run Tests

```bash
# All tests
dotnet test

# With code coverage
dotnet test --collect:"XPlat Code Coverage"

# Specific project
dotnet test Explore.Application.UnitTests

# Watch mode (auto-run on file changes)
dotnet watch test --project Explore.Application.UnitTests
```

### Test Structure

```
tests/
├── Explore.Application.UnitTests/
│   ├── Features/
│   │   └── Events/
│   │       ├── CreateEventCommandHandlerTests.cs
│   │       └── GetEventListRequestHandlerTests.cs
│   └── Validators/
│       └── CreateEventDtoValidatorTests.cs
│
└── Explore.API.IntegrationTests/
    └── Controllers/
        └── EventControllerTests.cs
```

### Writing Tests

**Unit Test Example:**
```csharp
public class CreateEventCommandHandlerTests
{
    private readonly Mock<IEventRepository> _eventRepository;
    private readonly Mock<IMapper> _mapper;
    private readonly CreateEventCommandHandler _handler;

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessResponse()
    {
        // Arrange
        var command = new CreateEventCommand { EventDto = new CreateEventDto { ... } };
        _eventRepository.Setup(x => x.Create(It.IsAny<Event>()))
            .ReturnsAsync(new Event { Id = Guid.NewGuid() });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Id.Should().NotBeEmpty();
    }
}
```

---

## 🔧 Development Workflow

### 1. Create a Feature Branch

```bash
git checkout -b feature/my-awesome-feature
```

### 2. Follow the Patterns

**Adding a new entity? Follow this checklist:**

- [ ] Create entity in `Explore.Domain/`
- [ ] Create DTOs in `Explore.Application/DTOs/{Entity}/`
- [ ] Create validators in `Explore.Application/DTOs/{Entity}/Validators/`
- [ ] Create repository interface in `Explore.Application/Contracts/Persistence/`
- [ ] Create repository implementation in `Explore.Persistence/Repositories/`
- [ ] Create entity configuration in `Explore.Persistence/Configurations/`
- [ ] Add DbSet to `ExploreDbContext.cs`
- [ ] Create migration: `dotnet ef migrations add Add{Entity}`
- [ ] Create commands in `Explore.Application/Features/{Entities}/Requests/Commands/`
- [ ] Create queries in `Explore.Application/Features/{Entities}/Requests/Queries/`
- [ ] Create handlers in `Explore.Application/Features/{Entities}/Handlers/`
- [ ] Create AutoMapper profile in `Explore.Application/Profiles/MappingProfile.cs`
- [ ] Create controller in `Explore.API/Controllers/{Entity}Controller.cs`
- [ ] Write tests in `Explore.Application.UnitTests/`

### 3. Critical Rules (Never Violate)

| Rule | ✅ Correct | ❌ Wrong |
|------|-----------|---------|
| **Repositories return entities** | `Task<List<Event>>` | `Task<List<EventDto>>` |
| **Validators manual instantiation** | `new Validator(_repo)` | DI injection |
| **Commands return wrapped response** | `BaseCommandResponse<Guid>` | `Guid` |
| **GET = AllowAnonymous** | `[AllowAnonymous]` | `[Authorize]` |
| **Write = Authorize** | `[Authorize]` on POST/PUT/DELETE | `[AllowAnonymous]` |
| **File-scoped namespaces** | `namespace X;` | `namespace X { }` |
| **No default values in entities** | Set in handler | `= 0` in entity |

See [QUICK_REFERENCE.md](docs/QUICK_REFERENCE.md) for all 12 critical rules.

### 4. Database Migrations

```bash
# Add migration
dotnet ef migrations add AddEventSessions --project Explore.Persistence

# Update database
dotnet ef database update --project Explore.Persistence

# Rollback
dotnet ef database update PreviousMigrationName --project Explore.Persistence

# Generate SQL script
dotnet ef migrations script --project Explore.Persistence
```

### 5. Code Quality Checks

```bash
# Format code
dotnet format

# Run linter
dotnet build /p:EnforceCodeStyleInBuild=true

# Check for security vulnerabilities
dotnet list package --vulnerable
```

---

## 🐳 Docker Development

### Development with Docker Compose

```yaml
# docker-compose.override.yml (for local dev)
version: '3.8'

services:
  api:
    build:
      context: .
      dockerfile: Explore.API/Dockerfile
      target: development
    volumes:
      - .:/app
      - /app/bin
      - /app/obj
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    command: ["dotnet", "watch", "run"]
```

```bash
# Hot reload enabled
docker-compose -f docker-compose.yml -f docker-compose.override.yml up
```

### Useful Docker Commands

```bash
# View logs
docker-compose logs -f api

# Restart specific service
docker-compose restart api

# Run migrations in container
docker-compose exec api dotnet ef database update

# Access PostgreSQL
docker-compose exec postgres psql -U postgres -d explore

# Clean rebuild
docker-compose down -v
docker-compose build --no-cache
docker-compose up -d
```

---

## 🔍 Debugging Tips

### Visual Studio / Rider

1. Set `Explore.AppHost` as startup project
2. Press F5 to debug all projects simultaneously
3. Aspire Dashboard shows all services at https://localhost:17225

### VS Code

```json
// .vscode/launch.json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Launch API",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/Explore.API/bin/Debug/net10.0/Explore.API.dll",
      "args": [],
      "cwd": "${workspaceFolder}/Explore.API",
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

### Common Issues

| Problem | Solution |
|---------|----------|
| Port already in use | Change port in `launchSettings.json` or kill process |
| Database connection failed | Check PostgreSQL is running: `docker-compose ps` |
| Migration failed | Delete migration and recreate: `dotnet ef migrations remove` |
| Build errors after git pull | Clean: `dotnet clean && dotnet restore && dotnet build` |
| Keycloak redirect issues | Check redirect URIs in Keycloak admin console |

---

## 📖 Documentation

### Must-Read for Contributors

| Document | What to Learn |
|----------|--------------|
| [CONTRIBUTING.md](docs/CONTRIBUTING.md) | How to submit PRs |
| [GOVERNANCE.md](docs/GOVERNANCE.md) | Coding standards |
| [QUICK_REFERENCE.md](docs/QUICK_REFERENCE.md) | 12 critical rules |
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | System design |
| [API.md](docs/API.md) | API conventions |

### Reference Docs

| Document | What's Inside |
|----------|--------------|
| [DOMAIN.md](docs/DOMAIN.md) | Entity relationships |
| [SECURITY.md](docs/SECURITY.md) | Auth/authz patterns |
| [BLAZOR.md](docs/BLAZOR.md) | Frontend architecture |
| [FEDERATION.md](docs/FEDERATION.md) | ATProto integration |
| [OPERATIONS.md](docs/OPERATIONS.md) | Deployment guide |

---

## 🤝 Contributing

### Getting Help

- **💬 Discord:** [Join our server](https://discord.gg/wrkY824Yv5) for real-time help
- **📖 Discussions:** [GitHub Discussions](https://github.com/islamu-ngo/Explore/discussions) for async questions
- **🐛 Issues:** [Report bugs](https://github.com/islamu-ngo/Explore/issues)

### Contribution Checklist

Before submitting a PR:

- [ ] Code follows [GOVERNANCE.md](docs/GOVERNANCE.md) conventions
- [ ] All tests pass: `dotnet test`
- [ ] Code is formatted: `dotnet format`
- [ ] No new warnings: `dotnet build`
- [ ] Documentation updated if needed
- [ ] PR description explains **what** and **why**

### PR Labels

| Label | When to Use |
|-------|------------|
| `bug` | Fixing a defect |
| `feature` | New functionality |
| `enhancement` | Improving existing feature |
| `docs` | Documentation changes |
| `refactor` | Code cleanup (no behavior change) |
| `tests` | Adding/fixing tests |
| `dependencies` | Package updates |

---

## 🚀 Deployment

### Quick Deploy with Coolify

1. Create new project in Coolify
2. Connect GitHub repository
3. Set build pack to `.NET`
4. Configure environment variables
5. Deploy!

### Docker Production

```bash
# Build production image
docker build -t explore-api -f Explore.API/Dockerfile .

# Run with production settings
docker run -d \
  -p 7001:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="your-prod-db" \
  explore-api
```

### Environment Variables

```bash
# Required
DATABASE_URL=postgresql://user:pass@host:5432/dbname
KEYCLOAK_URL=https://auth.example.com
KEYCLOAK_CLIENT_SECRET=your-secret

# Optional
SMTP_HOST=smtp.sendgrid.net
SMTP_PORT=587
SMTP_USER=apikey
SMTP_PASSWORD=your-api-key
MINIO_ENDPOINT=minio.example.com
MINIO_ACCESS_KEY=your-key
MINIO_SECRET_KEY=your-secret
```

See [OPERATIONS.md](docs/OPERATIONS.md) for complete deployment guide.

---

## 📊 Project Stats

![Build Status](https://img.shields.io/github/actions/workflow/status/islamu-ngo/Explore/build.yml?branch=main&style=flat-square)
![Code Coverage](https://img.shields.io/codecov/c/github/islamu-ngo/Explore?style=flat-square)
![GitHub Issues](https://img.shields.io/github/issues/islamu-ngo/Explore?style=flat-square)
![GitHub PRs](https://img.shields.io/github/issues-pr/islamu-ngo/Explore?style=flat-square)
![Contributors](https://img.shields.io/github/contributors/islamu-ngo/Explore?style=flat-square)

![Repository Stats](https://repobeats.axiom.co/api/embed/a0f11a3d9b80342b5f5965127c2c45871c9d3397.svg)

---

## 🙏 Contributors

<a href="https://github.com/islamu-ngo/explore/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=islamu-ngo/explore" />
</a>

**Thank you to everyone who has contributed!** 🎉

---

## 📞 Contact

- **💬 Discord:** https://discord.gg/wrkY824Yv5
- **🐛 Issues:** https://github.com/islamu-ngo/Explore/issues
- **📧 Email:** contact@openislamu.org

---

## 📄 License

**AGPL-3.0** — Open source forever, network use requires source disclosure.

See [LICENSE](LICENSE) for details.

---

## 🇵🇸 Support Palestine

[![Support Palestine](https://github.com/Safouene1/support-palestine-banner/blob/master/banner-support.svg)](https://www.palestinercs.org/en/Donation)

---

<div align="center">

**⭐️ Star this repo if you find it useful!**

[Docs](docs/) • [Roadmap](https://sites.plane.so/views/b8b7d9fced694f5a9d9a546e9d40d988) • [Discord](https://discord.gg/wrkY824Yv5) • [Contribute](docs/CONTRIBUTING.md)

**Built with ❤️ by developers, for developers**

</div>
