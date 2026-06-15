# Prompt Log — BKF-004: Hold Expiry Background Service

**Date:** 2026-06-14
**Unit:** [BKF-004 Hold Expiry Service](../ops/build/units/bkf-004-hold-expiry-service.md)
**Bolt:** BOLT-002
**Output Format:** Full implementation (C# infra BackgroundService + registration in Program.cs)

---

## Prompt Quality Gate

| Component | Value |
|---|---|
| **Context** | CabinConnect backend — BKF-004 Hold Expiry; C# .NET 10 BackgroundService with PeriodicTimer |
| **Constraints** | CLAUDE.md rules; IServiceScopeFactory pattern (BackgroundService is singleton, IHoldRepository is scoped); errors must not crash the host |
| **Acceptance Criteria** | 6 ACs from bkf-004-hold-expiry-service.md |
| **Output Format** | Infrastructure BackgroundService + domain interface extension + Program.cs registration |

---

## Files Generated

| File | Purpose |
|---|---|
| `src/backend/CabinConnect.Domain/Holds/IHoldRepository.cs` (updated) | Added `CancelExpiredHoldsAsync` |
| `src/backend/CabinConnect.Infrastructure/Holds/HoldRepository.cs` (updated) | Implements `CancelExpiredHoldsAsync` (single UPDATE query) |
| `src/backend/CabinConnect.Infrastructure/Holds/HoldExpiryService.cs` | BackgroundService; PeriodicTimer; `RunIterationAsync` internal for testability |
| `src/backend/CabinConnect.Api/Program.cs` (updated) | `AddHostedService<HoldExpiryService>()` |
| `src/backend/CabinConnect.Infrastructure/CabinConnect.Infrastructure.csproj` (updated) | Added `Microsoft.Extensions.Hosting.Abstractions` |

---

## AC Coverage

| AC | Coverage |
|---|---|
| AC-1 Expired Active Holds cancelled | SQL: `WHERE status = 'Active' AND expires_at < now()` |
| AC-2 Non-expired Holds unaffected | Same WHERE clause naturally excludes them |
| AC-3 Consumed/Cancelled Holds unaffected | `AND status = 'Active'` filter |
| AC-4 Job runs on configurable interval (default 5 min) | `HoldExpiryIntervalMinutes` config key; `PeriodicTimer` |
| AC-5 Count logged at Information level | `LogInformation("Hold expiry job: {Count} hold(s) cancelled", count)` |
| AC-6 Job failure does not crash the API | Try/catch in `RunIterationAsync`; logs Error, continues to next tick |

---

## Key Design Decisions

- **`IServiceScopeFactory` instead of `NpgsqlDataSource`**: `BackgroundService` is a singleton; `IHoldRepository` is scoped. Injecting `NpgsqlDataSource` directly into the service caused the "missing connection string" guard to fire at startup during tests (the factory lambda throws if connection string is null). Using `IServiceScopeFactory` defers resolution to the per-tick scope — tests can mock `IHoldRepository` without needing a real database.
- **`CancelExpiredHoldsAsync` on `IHoldRepository`**: Keeps the SQL in the infrastructure layer consistent with the repository pattern rather than mixing raw data access into the service.
- **`internal RunIterationAsync`**: Exposes the core logic (without the PeriodicTimer loop) for direct testing without mocking a timer.
