# Prompt Log — CSA-002 Cabin Availability Query API

**Date:** 2026-06-14
**Unit:** [CSA-002](../ops/build/units/csa-002-cabin-availability-query-api.md)
**Bolt:** [BOLT-001](../ops/build/bolts/2026-06-12-bolt-001-cabin-search.md)
**Engineer:** RP
**AI Model:** claude-sonnet-4-6

---

## Prompt Quality Gate

| Component | Value |
|---|---|
| **Context** | CSA-002 — Availability Query API, backend API layer, consumes CSA-001 NightlyRateCalculator |
| **Constraints** | Repository pattern, DTOs at API boundary, no .Result/.Wait(), public endpoint, EC-003/004/009/010 |
| **Acceptance Criteria** | 15 ACs — availability filter, blackout dates, price breakdown, filters, sorting, pagination, validation; AC-15 (performance) excluded from unit tests |
| **Output Format** | Domain models + CabinSearchService + ICabinSearchRepository + Infrastructure stub + CabinsController + API models + unit tests (NSubstitute) + integration tests (WebApplicationFactory) |

---

## Prompts Issued

### 1 — Start unit
**Prompt:** "start CSA-002"
**Output:** Read CSA-002 unit file in full; installed NSubstitute + Microsoft.AspNetCore.Mvc.Testing; created 16 source files across Domain, Infrastructure, and Api projects; created domain service tests (12 tests) and API integration tests (6 tests). All 25 tests passed on first run.

---

## Output Summary

### Files created

**Domain:**
| File | Purpose |
|---|---|
| `Cabins/Cabin.cs` | Cabin domain model |
| `Cabins/CabinLocation.cs` | Location value record |
| `Cabins/BlackoutDate.cs` | Blackout date entity |
| `Bookings/Booking.cs` | Booking entity (for availability check) |
| `Bookings/BookingStatus.cs` | Pending/Confirmed/Cancelled/Completed/NoShow |
| `Common/PagedResult.cs` | Generic pagination wrapper |
| `Search/CabinWithAvailabilityData.cs` | Repo return type: cabin + overlapping bookings + blackouts |
| `Search/ICabinSearchRepository.cs` | Repository interface |
| `Search/CabinSearchQuery.cs` | Service input record |
| `Search/RateLineItemResult.cs` | Grouped rate line for API response |
| `Search/CabinSearchResult.cs` | Service output record (matches API contract) |
| `Search/CabinSearchService.cs` | Core service: availability + filtering + pricing + sorting + pagination |

**Infrastructure:**
| File | Purpose |
|---|---|
| `Search/CabinSearchRepository.cs` | Stub — returns empty list; real implementation deferred to DB unit |

**Api:**
| File | Purpose |
|---|---|
| `Models/ApiResponse.cs` | Generic `{ data, error }` envelope |
| `Models/ApiError.cs` | `{ code, message }` error record |
| `Models/CabinSearchRequest.cs` | Query string binding model |
| `Controllers/CabinsController.cs` | GET /api/cabins/search — validation, parsing, service call |

### Test results
```
Domain.Tests:  19 passed (12 CSA-002 + 7 CSA-001)
Api.Tests:      6 passed
Total:         25 passed, 0 failed
```

---

## Review Checklist Result

All items passed. Key notes:
- EC-003: dates parsed with `DateOnly.TryParseExact("yyyy-MM-dd")` — no timezone, date-only (AC-9)
- EC-004: blackout dates checked in service; repo pre-filters overlapping range (AC-2)
- EC-009: `checkOut < checkIn` → 400 INVALID_DATE_RANGE (AC-7)
- EC-010: `checkIn == checkOut` → 400 ZERO_NIGHT_STAY checked before less-than (AC-8)
- AC-10: `[AllowAnonymous]` on Search endpoint — explicitly public
- AC-11: `IsPublished` guard in service (repo also pre-filters)
- AC-15 (performance): excluded from unit tests — requires seeded 10k-cabin DB; flagged as a separate validation step before bolt close

---

## Decisions & Trade-offs

| Decision | Rationale |
|---|---|
| Repository returns cabins with pre-filtered overlapping data | DB-level date-range join is more efficient than loading all bookings; real SQL uses overlap condition: `CheckIn < @checkOut AND CheckOut > @checkIn` |
| Filtering and sorting in-memory in the service | Correct for a stub; real implementation moves filter predicates into the SQL query for performance (AC-15) |
| `GroupLineItems` in service (not domain) | Grouping is a presentation concern tied to the API response shape; NightlyRateCalculator stays pure |
| `public partial class Program` in Program.cs | Required for WebApplicationFactory to access the entry point in integration tests |
| AC-15 deferred | Performance test requires a seeded 10k-cabin dataset against a real DB — out of scope for unit test run, must be validated manually before bolt close |
