# Prompt Log — BKF-009: Host Booking Management

**Date:** 2026-06-15
**Unit:** [BKF-009 Host Booking Management](../ops/build/units/bkf-009-host-booking-management.md)
**Bolt:** BOLT-002
**Output Format:** Full implementation (SQL migration + C# domain/infra/API + React page + tests)

---

## Prompt Quality Gate

| Component | Value |
|---|---|
| **Context** | CabinConnect — BKF-009 Host Booking Management; backend `GET /api/host/bookings` + frontend `/host/bookings` |
| **Constraints** | CLAUDE.md rules; `cabins.host_id` migration included in scope; view-only (no cancel/reject); EC-007 host sees only their own cabins' bookings; guest shown as truncated ID only |
| **Acceptance Criteria** | 9 ACs from bkf-009-host-booking-management.md |
| **Output Format** | SQL migration, domain record, repository method, new HostController, React page, xUnit tests |

---

## Files Generated

| File | Purpose |
|---|---|
| `infra/database/migrations/003_cabins_add_host_id.sql` | Adds `host_id uuid` to `cabins`; RLS update/delete policies; index |
| `src/backend/CabinConnect.Domain/Bookings/HostBookingItem.cs` | New record with `GuestId` field |
| `src/backend/CabinConnect.Domain/Bookings/IBookingRepository.cs` (updated) | Added `GetHostBookingsAsync` |
| `src/backend/CabinConnect.Domain/Bookings/BookingService.cs` (updated) | Delegation method |
| `src/backend/CabinConnect.Infrastructure/Bookings/BookingRepository.cs` (updated) | JOIN `cabins ON host_id = @HostId`; optional `@Status` filter |
| `src/backend/CabinConnect.Api/Controllers/HostController.cs` | `GET /api/host/bookings` — Authorize; status filter; truncated guestRef |
| `src/frontend/cabin-connect/src/features/host/host-bookings-page.tsx` | `/host/bookings` — table + status dropdown + empty state + RequireAuth |
| `src/App.tsx` (updated) | Added `/host/bookings` route |
| `tests/CabinConnect.Api.Tests/Controllers/HostControllerTests.cs` | 4 tests covering AC-1/AC-2/AC-4 |

---

## AC Coverage

| AC | Status |
|---|---|
| AC-1 Returns all Bookings for Host's Cabins | `JOIN cabins ON host_id = @HostId` |
| AC-2 Status filter applied | `WHERE (@Status IS NULL OR b.status = @Status)` |
| AC-3 Host sees only their own Cabins' Bookings | `host_id = @HostId` in JOIN condition |
| AC-4 Unauthenticated → 401 | `[Authorize]` on controller; test `GetHostBookings_NoAuth_Returns401` |
| AC-5 Paginated results | `LIMIT/OFFSET` + `COUNT(*) OVER()` |
| AC-6 Booking table displayed | Table with cabin, dates, guestRef, price, status |
| AC-7 Status filter UI | `<select>` dropdown; on change reloads with `?status=…` |
| AC-8 Empty state | Rendered when `items.length === 0` |
| AC-9 Auth guard | Wrapped in `<RequireAuth>` |

---

## Key Design Decisions

- **`guestRef` as truncated UUID**: `b.GuestId.ToString()[..8] + "…"` — avoids joining `auth.users` which is outside the Supabase RLS boundary for this intent. Full guest details are deferred.
- **Separate `HostController`**: Different route prefix (`/api/host/…`) and conceptually different actor (Host vs Guest). Keeps `BookingsController` focused on Guest operations.
- **Status filter as nullable string → `BookingStatus?`**: Parsed in the controller with a 400 on unrecognised values; passed as `status?.ToString()` to SQL so `WHERE (@Status IS NULL OR b.status = @Status)` handles both "all" and filtered cases in one query.
