Searched codebase for "write a task for this repository to write an e2e tests for the back end and ensure that core functionality works as expected #codebase ", 19 results

I’ll draft a clear, actionable GitHub task you can paste as an issue to implement backend end-to-end tests that verify core flows across the API.

## Title

Add end-to-end tests for core backend flows to validate API behavior

## Labels

- Area: Tests
- Type: E2E
- Priority: High
- #codebase

## Summary

Create and run E2E tests for the Todo API to exercise real HTTP calls against a test host, covering happy-path and critical edge cases for Users, TodoItems, Tags, Comments, and Activity Logs. Tests should validate status codes, contracts, and side effects (e.g., activity log entries, authorization boundaries).

## Motivation

- Confidence in core flows when refactoring or adding features
- Catch regressions beyond unit boundaries (routing, filters, DI, middleware, db)
- Establish a repeatable baseline for PR validation in CI

## Scope

Implement E2E tests in `TodoApi.Tests.E2E` that:

- Boot the Web API in a test host (in-memory) and exercise endpoints with real HTTP
- Override the data store to be ephemeral and isolated per test run
- Seed minimal data and assert on persisted results
- Validate error handling (400/401/403/404) and success paths
- Ensure activity logging works for key mutations

End-to-end flows to cover:

1. Users
   - Register user, update profile, fetch by id/me
   - Negative: invalid registration payload
2. Auth/Authorization
   - Acquire auth token/session if applicable
   - Access control: a user cannot access or mutate another user’s resources
3. TodoItems
   - Create, get by id, list, update title/description/priority, complete/archive
   - Filtering/pagination if exposed (priority/status/tag)
   - Negative: invalid payload, not found, forbidden
4. Tags
   - Create tags, attach/detach to a todo item, list tags of item
5. Comments
   - Add comment to a todo item, list comments, delete if authorized
6. ActivityLog
   - Verify entries created for create/update/comment actions (type and metadata)

Out of scope:

- UI tests
- Performance/load tests
- Non-HTTP integration tests (repositories covered elsewhere)

## Technical Approach

- Test host: use `WebApplicationFactory<TProgram>` with `Microsoft.AspNetCore.Mvc.Testing`.
- Database strategy:
  - Option A (default): EF Core SQLite in-memory with shared connection per test class; migrations applied on startup.
  - Option B: EF Core InMemory for faster iteration (use only if SQLite cannot be wired quickly).
  - Option C (optional follow-up): Testcontainers with the same DB engine used in prod for highest fidelity.
- Override DI for tests: subclass `WebApplicationFactory` to replace `ApplicationDbContext` with the test provider.
- Seed data: add a small seeding helper to create users, todos, and tags in test DB context prior to requests.
- HTTP client: use `factory.CreateClient()`; wire auth headers if the API requires JWT/bearer tokens. If auth is not finalized, add a test auth handler stub for E2E.
- Assertions: status codes, response bodies (DTO shapes), and cross-entity effects (e.g., activity log created).

## Proposed Test Project Layout

Within `TodoApi.Tests.E2E`:

- Infrastructure/
  - `CustomWebApplicationFactory.cs` (overrides DB, optional auth)
  - `DbSeeder.cs` (test data setup)
  - `HttpClientExtensions.cs` (helpers: auth, content)
- UsersEndpointsTests.cs
- TodoItemsEndpointsTests.cs
- TagsEndpointsTests.cs
- CommentsEndpointsTests.cs
- ActivityLogEndpointsTests.cs

Note: `CommentsEndpointsTests.cs` already exists—expand it and add new test files above.

## Packages to Add (Test Project)

- Microsoft.AspNetCore.Mvc.Testing
- Microsoft.AspNetCore.TestHost
- FluentAssertions
- Microsoft.Data.Sqlite (if using SQLite in-memory)
- Microsoft.EntityFrameworkCore.Sqlite
- (Optional) DotNet.Testcontainers if selecting Option C

## Test Cases Matrix (high-level)

- Users
  - POST /api/users (201, returns id)
  - PUT /api/users/{id} (200, changes visible via GET)
  - GET /api/users/{id} (200), GET /api/users/{otherId} as different user (403)
  - Invalid body (400), not found (404)
- Auth
  - If applicable: POST /api/auth/login -> token; subsequent authorized calls use bearer
  - Unauthorized access without token (401)
- TodoItems
  - POST /api/todoitems (201) -> GET same id (200) -> PATCH status to complete (200) -> GET list with filter (200)
  - PUT invalid (400), GET non-existent (404), access another user’s item (403)
- Tags
  - POST /api/tags (201), POST /api/todoitems/{id}/tags/{tagId} (204), GET /api/todoitems/{id}/tags (200)
  - Remove tag (204)
- Comments
  - POST /api/todoitems/{id}/comments (201), GET /api/todoitems/{id}/comments (200), DELETE comment by owner (204), by non-owner (403)
- ActivityLog
  - After create/update/comment: GET /api/activity (or /api/todoitems/{id}/activity if applicable) -> contains expected events with correct `ActivityEventType`

## Acceptance Criteria

- E2E tests run with `dotnet test` and pass locally.
- Test host boots with an ephemeral database per test class or per test run.
- Coverage across all flows listed in scope with both success and key failure modes.
- Activity log assertions exist for at least create/update/comment flows.
- Authorization tests prove isolation between users.
- CI-ready: tests are deterministic and do not depend on external services.

## Validation Steps

- Build: `dotnet build TodoApi/Api.sln`
- Run API locally (smoke only, optional): `dotnet run --project TodoApi/TodoApi.WebApi`
- Tests: `dotnet test`
- Review test logs for clear assertion messages; avoid flakiness (random data seeded deterministically where needed).

## Risks and Mitigations

- Unknown DB provider: default to SQLite in-memory; upgrade to Testcontainers later if needed.
- Auth not finalized: introduce a test-only authentication scheme to mint identities for E2E.
- Flaky timing-dependent tests: avoid polling; assert via DB state or immediate response bodies.

## Definition of Done

- New and updated E2E tests added under `TodoApi.Tests.E2E` with clear naming and structure.
- Tests pass locally and are suitable for CI.
- Brief README note added to repository root or test project readme with how-to-run and troubleshooting (e.g., SQLite requirement).
- PR includes test evidence and aligns with AGENTS.md commit/PR guidelines.

## References

- Repo build/test commands in AGENTS.md
- Existing `CommentsEndpointsTests.cs` in `TodoApi.Tests.E2E` for placement
- `ApplicationDbContext` in `TodoApi.Infrastructure/Data` for DB override hints

## Estimated Effort

- 1–2 days to wire the factory/db override + core happy-path tests
- +1 day for negative cases and activity log coverage
- +0.5 day if test auth scheme is required

If you want, I can scaffold the `CustomWebApplicationFactory` with SQLite in-memory and a first `TodoItemsEndpointsTests` file next.
