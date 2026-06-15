# Backlog

All units across all intents. Update status here whenever a unit moves.

**Statuses:** `Draft` | `Ready` | `In Progress` | `Done` | `Deferred`

---

## Cabin Search & Availability

Intent: [2026-06-11-cabin-search-availability.md](../inception/intents/2026-06-11-cabin-search-availability.md)
Elaboration: [2026-06-12-session-1.md](../inception/elaborations/cabin-search-availability/2026-06-12-session-1.md)
Bolt: [BOLT-001](bolts/2026-06-12-bolt-001-cabin-search.md) — Planned

| ID | Unit | Layer | Priority | Status |
|---|---|---|---|---|
| [CSA-001](units/csa-001-nightly-rate-calculator.md) | Nightly Rate Breakdown Calculator | Backend — Domain | High | Done |
| [CSA-002](units/csa-002-cabin-availability-query-api.md) | Cabin Availability Query API | Backend — API | High | Done |
| [CSA-003](units/csa-003-search-form-component.md) | Search Form Component | Frontend | High | Done |
| [CSA-004](units/csa-004-cabin-result-card.md) | Cabin Result Card | Frontend | High | Done |
| [CSA-005](units/csa-005-search-results-list-view.md) | Search Results List View | Frontend | High | Done |
| [CSA-006](units/csa-006-map-view.md) | Map View (Optional Toggle) | Frontend | Low | Ready |

---

## Booking Flow

Intent: [2026-06-14-booking-flow.md](../inception/intents/2026-06-14-booking-flow.md)
Elaboration: [2026-06-14-session-1.md](../inception/elaborations/booking-flow/2026-06-14-session-1.md)
Bolt: [BOLT-002](bolts/2026-06-14-bolt-002-booking-flow.md) — Planned

| ID | Unit | Layer | Priority | Status |
|---|---|---|---|---|
| [BKF-001](units/bkf-001-place-hold.md) | Place Hold | Backend — API | High | Done |
| [BKF-002](units/bkf-002-confirm-booking.md) | Confirm Booking | Backend — API | High | Done |
| [BKF-003](units/bkf-003-cancel-booking.md) | Cancel Booking | Backend — API | High | Done |
| [BKF-004](units/bkf-004-hold-expiry-service.md) | Hold Expiry Background Service | Backend — Infrastructure | High | Done |
| [BKF-005](units/bkf-005-cabin-detail-page.md) | Cabin Detail Page | Frontend | High | Done |
| [BKF-006](units/bkf-006-checkout-page.md) | Checkout Page | Frontend | High | Done |
| [BKF-007](units/bkf-007-booking-confirmation-page.md) | Booking Confirmation Page | Frontend | Medium | Done |
| [BKF-008](units/bkf-008-guest-my-bookings.md) | Guest My Bookings | Backend + Frontend | Medium | Done |
| [BKF-009](units/bkf-009-host-booking-management.md) | Host Booking Management | Backend + Frontend | Low | Done |

---

## User Identity & Roles

Intent: *(to be written)*
Bolt: *(to be planned)*

| ID | Unit | Layer | Priority | Status |
|---|---|---|---|---|
| UIR-001 | User Profiles DB + Role System | Backend — DB + API | High | Done |
| UIR-002 | Registration Page (Guest / Host) | Frontend | High | Done |
| UIR-003 | Site-wide Navigation Bar | Frontend | High | Done |
