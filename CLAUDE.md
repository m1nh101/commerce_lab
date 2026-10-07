# CLAUDE.md

CommerceHub: a learning e-commerce backend built with .NET 10. Today there is one service, **Product Catalog** (`src/api/product-catalog`). `src/lib/CommerceHub.Configuration` is an early shared library and is not yet tracked in git.

## Commands
Run these from `src/api/product-catalog` unless a step says otherwise.
- Start the infrastructure (from the repo root): `docker compose up -d`. This starts Postgres :5432, RabbitMQ :5672/:15672, Redis :6379 and LocalStack (S3, SES) :4566. Credentials come from `.env` (template: `.example.env`).
- Build: `dotnet build ProductCatalog.slnx`
- Run the API: `dotnet run --project CommerceHub.ProductCatalog`. In Development it serves the OpenAPI document at `/openapi/v1.json` and the Scalar UI at `/scalar`.
- Restore tools (from the repo root): `dotnet tool restore`. This installs dotnet-ef 10.0.12.
- Add a migration:
  `dotnet ef migrations add <Name> --project CommerceHub.ProductCatalog.Infrastructure --startup-project CommerceHub.ProductCatalog --output-dir Database/Migrations`
- Apply migrations: `dotnet ef database update --project CommerceHub.ProductCatalog.Infrastructure --startup-project CommerceHub.ProductCatalog`
- There are no test projects yet.

The connection string `ConnectionStrings:ProductCatalog` is required, and startup throws if it is missing. It is not in `appsettings.json`, so supply it through user-secrets or an environment variable.

## Architecture (Clean Architecture)
Dependencies point inward: Api → Application → Domain. Infrastructure → Application + Domain.
- **Domain**: entities with private setters, a private ctor and static `Create(...)` factories, and validation through `Guard`. Base types are `Entity<TId>`, `AuditableEntity` and the `IAggregateRoot` marker. Max-length constants live on the entity (e.g. `Category.NameMaxLength`).
- **Application**: one folder per feature (`Categories/`) holding `I<Feature>Service`, an `internal sealed` service, DTOs, `*Inputs`, `*Errors` and query extensions. Data access goes through `IProductCatalogDbContext`, using EF Core directly with no repositories. Registration happens in `DependencyInjection.AddApplication()`.
- **Infrastructure**: `ProductCatalogDbContext` on Npgsql with `UseSnakeCaseNamingConvention()`. There is one `IEntityTypeConfiguration` per entity in `Database/Configurations`. `AuditableEntityInterceptor` sets audit columns using `TimeProvider`.
- **Api** (`CommerceHub.ProductCatalog`): Minimal API endpoint classes in `Endpoints/<Feature>/` exposed as `Map<Feature>Endpoints()` and called from `Program.cs`. Request records live next to them.

## Conventions
- **Errors**: services return `Result` or `Result<T>` and never throw for business failures. Use `Error(Code, Message, ErrorType)`, with feature errors collected in `<Feature>Errors`. Implicit conversions let you write `return error;` or `return value;`.
- **HTTP responses**: always use `ApiResults.*` (`Common/ApiResponse.cs`). The envelope is `{ success, data, pagination, message, error }`. Error types map to status codes: NotFound→404, Conflict→409, Validation→400.
- **JSON**: snake_case property names, and enums are serialized as snake_case strings (integers are rejected).
- **Routing**: `api/v{version}/<resource>`, using an Asp.Versioning version set (currently v1.0).
- **Pagination**: `PagedResult<T>`, with a maximum page size of 100.
- **DB**: snake_case tables and columns. Migrations live in `Infrastructure/Database/Migrations`.

## Specs first
Feature behavior is defined in `docs/spec/product/*.md` (categories, products, variants, attributes, images) and the schema in `docs/database/product.md`. Read the matching spec before you implement or change an endpoint, and keep the code consistent with it.

## Git
Use Conventional Commits (`feat:`, `docs:`, …).
