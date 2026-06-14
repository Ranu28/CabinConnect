# Unit: Hold Expiry Background Service

**ID:** BKF-004
**Status:** Ready
**Intent:** [Booking Flow](../../inception/intents/2026-06-14-booking-flow.md)
**Layer:** Backend — Infrastructure
**Priority:** High
**Date:** 2026-06-14

---

## Purpose

A background service that runs every 5 minutes and cancels all Active Holds whose `expiresAt` has passed, returning the cabin dates to availability without manual intervention.

## Acceptance Criteria

**AC-1 — Expired Active Holds cancelled:**
- **Given** one or more Holds with `status=Active` and `expiresAt < now()`
- **When** the job runs
- **Then** each such Hold transitions to `status=Cancelled`

**AC-2 — Non-expired Holds unaffected:**
- **Given** a Hold with `status=Active` and `expiresAt > now()`
- **When** the job runs
- **Then** the Hold remains `Active` and unchanged

**AC-3 — Consumed and Cancelled Holds unaffected:**
- **Given** a Hold with `status=Consumed` or `status=Cancelled`
- **When** the job runs regardless of `expiresAt`
- **Then** the Hold is not modified

**AC-4 — Job runs on a 5-minute interval:**
- **Given** the API is running
- **Then** the Hold expiry job executes every 5 minutes, configurable via `HoldExpiryIntervalMinutes` in app settings (default: 5)

**AC-5 — Job is logged:**
- **Given** the job runs
- **Then** the count of Holds cancelled in that run is written to the application log at `Information` level; zero cancellations is also logged

**AC-6 — Job failure does not crash the API:**
- **Given** a transient DB error during the job run
- **When** the job encounters the error
- **Then** the error is caught, logged at `Error` level, and the job reschedules normally for the next interval; the API continues serving requests

## Edge Cases Addressed

- EC-002 — Expiry is based on `expiresAt` timestamp, not on client-side timers. Even if a Guest's UI shows the countdown, the server is authoritative.

## Dependencies

- BKF-001 — Holds table must exist

## Implementation Notes

- Use `IHostedService` / `BackgroundService` with a `PeriodicTimer` (or `System.Threading.PeriodicTimer` in .NET 6+).
- The cancellation query: `UPDATE holds SET status = 'Cancelled' WHERE status = 'Active' AND expires_at < now()` — single atomic statement, no row-by-row processing needed.
- Register as a singleton hosted service in `Program.cs`.
- Interval is read from `IConfiguration["HoldExpiryIntervalMinutes"]`; defaults to `5` if absent.
- No unit tests for the scheduler timer itself; test the cancellation query logic in an integration test against a real DB (future Testcontainers intent). AC-1–AC-3 are covered by repository unit tests using mock data.
