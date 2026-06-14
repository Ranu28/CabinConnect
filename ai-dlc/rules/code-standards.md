# Code Standards

## Languages & Runtimes
- **Backend:** C# / .NET 8+
- **Frontend:** React 18+ with TypeScript (strict mode enabled)
- **Database:** PostgreSQL via Supabase

## Naming Conventions
- C#: PascalCase for types/methods, camelCase for locals/params, `_camelCase` for private fields
- TypeScript/React: PascalCase for components, camelCase for functions/variables, UPPER_SNAKE_CASE for constants
- Database: snake_case for tables and columns
- Files: kebab-case for React files (`cabin-card.tsx`), PascalCase for C# files (`CabinService.cs`)

## Framework Patterns
### Backend (.NET)
- Minimal API or Controller-based — pick one per service, do not mix
- Use repository pattern for data access; no raw SQL in controllers
- **Npgsql + Dapper array parameters:** use `= ANY(@Param)` — never `IN @Param`. Dapper's `IN` expansion is SQL Server–style and produces `IN $1` under Npgsql, which is a PostgreSQL syntax error (42601) at runtime. `= ANY(@Param)` works natively with PostgreSQL array types and is the correct idiom.
- DTOs at API boundaries; domain models stay internal
- Async/await throughout — no `.Result` or `.Wait()`
- Use `BadRequest(ModelState)` for model validation errors in controllers — **not** `ValidationProblem(ModelState)`; `ValidationProblem` requires `ProblemDetailsFactory` which is unavailable outside the ASP.NET Core pipeline and silently returns a null status code in unit tests

### Frontend (React)
- Functional components only — no class components
- Co-locate styles, tests, and types with the component
- Custom hooks for shared stateful logic (`use-cabin-search.ts`)
- No prop drilling beyond two levels — use context or a state manager
- **Auth redirects:** do not call `navigate()` imperatively after `signInWithPassword()` — the session hasn't updated yet and will cause a redirect loop; instead rely on `onAuthStateChange` driving reactive state and use `<Navigate to={from} replace />` in the render path

## Formatting & Linting
- Backend: enforced by `.editorconfig` + Roslyn analyzers
- Frontend: ESLint + Prettier — run `npm run lint` before committing
- No `any` in TypeScript without an explicit comment explaining why

## Testing
- Unit tests required for all business logic
- Integration tests for all API endpoints
- Frontend: React Testing Library — test behavior, not implementation
- Minimum 80% coverage on the domain layer
