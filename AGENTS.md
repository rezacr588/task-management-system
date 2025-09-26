# Repository Guidelines

## Project Structure & Module Organization
- Root contains `README.md`, `AGENTS.md`, and the `TodoApi` solution folder; keep documentation at root for quick onboarding.
- `TodoApi/Api.sln` wires four projects: `TodoApi.Domain` (entities, interfaces), `TodoApi.Application` (DTOs, services, mapping), `TodoApi.Infrastructure` (EF Core DbContext, repositories), and `TodoApi.WebApi` (ASP.NET endpoints).
- Generated artifacts live under each project’s `bin/` and `obj/`; do not commit them.
- CI definitions reside in `.github/workflows`; mirror new automation here.

## Build, Test, and Development Commands
- `dotnet restore TodoApi/Api.sln` – downloads NuGet dependencies.
- `dotnet build TodoApi/Api.sln` – compiles all projects with warnings as errors.
- `dotnet run --project TodoApi/TodoApi.WebApi` – hosts the HTTP API on the configured port.
- `dotnet watch --project TodoApi/TodoApi.WebApi run` – auto-rebuilds while editing during local development.
- `dotnet test` – executes the full test suite once projects such as `TodoApi.Tests` are added.

## Coding Style & Naming Conventions
- Follow standard .NET conventions: 4-space indentation, braces on new lines, expression-bodied members only when they improve clarity.
- Use PascalCase for classes, interfaces (prefixed with `I`), methods, and public properties; camelCase for fields, locals, and parameters.
- Async members should end with `Async`; mimicking existing services improves discoverability.
- Place new files beside their peers (e.g., new repository implementations under `TodoApi.Infrastructure/Repositories`).

## Testing Guidelines
- Adopt xUnit for unit tests and FluentAssertions for expressiveness; integration tests can leverage `WebApplicationFactory`.
- Organize tests into `TodoApi.Tests/<Area>` matching the project under test.
- Name test methods `MethodName_StateUnderTest_ExpectedResult` and keep Arrange/Act/Assert sections explicit.
- Run `dotnet test` locally before pushing; target high coverage on application services and domain invariants.

## Commit & Pull Request Guidelines
- Prefer imperative subject lines ("Add comment repository") mirroring the existing history (`git log --oneline`).
- Include a brief body describing motivation and noteworthy changes; reference issues with `Refs #123`.
- PRs must summarize scope, list validation steps (build, test runs), attach screenshots for UI/API contract updates, and note any required migrations.
