# Bolt: Booking Flow — Hold to Confirmed Booking

**ID:** BOLT-002
**Status:** Planned
**Dates:** 2026-06-14 → 2026-06-28 (planned)
**Driver:** RP
**Intent(s):** [Booking Flow](../../inception/intents/2026-06-14-booking-flow.md)

---

## Goal

A Guest can place a Hold on an available cabin, confirm it into a Booking with a frozen total price, cancel the Booking if needed, and view their booking history — with expired Holds cleaned up automatically and Hosts able to view all Bookings across their cabins.

## Units in Scope

| Order | Unit | Layer | Priority | Status at Plan | Notes |
|---|---|---|---|---|---|
| 1 | [BKF-001](../units/bkf-001-place-hold.md) | Backend — API | High | Ready | Foundation: creates `holds` table, Place Hold endpoint, updates CSA-002 to exclude active Holds. Everything gates on this. |
| 2 | [BKF-002](../units/bkf-002-confirm-booking.md) | Backend — API | High | Ready | Parallel with BKF-004 and BKF-005 (mocked). Creates `bookings` table and atomic Hold→Booking transaction (EC-012). |
| 2 | [BKF-004](../units/bkf-004-hold-expiry-service.md) | Backend — Infrastructure | High | Ready | Parallel with BKF-002. Needs only the `holds` table schema from BKF-001. |
| 2 | [BKF-005](../units/bkf-005-cabin-detail-page.md) | Frontend | High | Ready | Parallel with BKF-002/BKF-004. Can build with mocked Hold API; wire to real BKF-001 endpoint when available. Includes `GET /api/cabins/{id}`. |
| 3 | [BKF-003](../units/bkf-003-cancel-booking.md) | Backend — API | High | Ready | Needs bookings to exist (BKF-002). Idempotent cancel with audit trail. |
| 3 | [BKF-006](../units/bkf-006-checkout-page.md) | Frontend | High | Ready | Needs BKF-001 (holdId in URL) and BKF-002 (confirm endpoint). Live countdown from server `expiresAt`. |
| 4 | [BKF-007](../units/bkf-007-booking-confirmation-page.md) | Frontend | Medium | Ready | Needs BKF-006 (navigated from checkout). Read-only success page. |
| 4 | [BKF-008](../units/bkf-008-guest-my-bookings.md) | Backend + Frontend | Medium | Ready | Needs BKF-002 (bookings exist) and BKF-003 (cancel action). `GET /api/bookings` + `/my-bookings` page. |
| 5 | [BKF-009](../units/bkf-009-host-booking-management.md) | Backend + Frontend | Low | Ready | **Stretch.** Requires `cabins.host_id` migration. Drop if BKF-001 through BKF-008 aren't solid with time remaining. |

## Build Sequence

Two parallel tracks from BKF-001, converging at BKF-006/BKF-007/BKF-008:

```
                     ┌──→ BKF-002 ──→ BKF-003 ──┐
                     │                           ├──→ BKF-008
BKF-001 ─────────────┤──→ BKF-004               │
                     │                           │
                     └──→ BKF-005 ──→ BKF-006 ──→ BKF-007
                                                 │
                                                 └──→ BKF-009 (stretch)
```

- **BKF-001 must be built first** — it creates the `holds` table schema, the Place Hold API, and the CSA-002 patch (exclude Active Holds from availability). All other units depend on this foundation.
- **Phase 2 (parallel after BKF-001):**
  - BKF-002: Confirm Booking — creates `bookings` table and the atomic Hold→Booking transaction
  - BKF-004: Hold Expiry Service — only needs the `holds` table; can run entirely in parallel
  - BKF-005: Cabin Detail Page — start with mocked `POST /api/holds`; wire to real endpoint once BKF-001 lands
- **Phase 3 (after BKF-002 is stable):**
  - BKF-003: Cancel Booking — needs bookings to exist
  - BKF-006: Checkout Page — needs both Hold API (holdId in URL) and Confirm Booking API
- **Phase 4 (converging):**
  - BKF-007: Booking Confirmation Page — trivial read-only page; needs BKF-006 to navigate to it
  - BKF-008: Guest My Bookings — backend + frontend; needs BKF-002 (data) and BKF-003 (cancel action)
- **Phase 5 (stretch):**
  - BKF-009: Host Booking Management — only start after BKF-001 through BKF-008 are complete and tested

## Definition of Done

- [ ] Every unit's acceptance criteria pass with tests (BKF-001: 10 ACs, BKF-002: 8, BKF-003: 9, BKF-004: 6, BKF-005: 7, BKF-006: 7, BKF-007: 6, BKF-008: 8)
- [ ] EC-011 (auto-cancel prior Hold) and EC-012 (atomic Hold+Booking transaction) explicitly covered by integration tests
- [ ] RLS policies confirmed on `holds` and `bookings` tables (Guest-scoped reads/writes)
- [ ] `holds` table excluded Active Hold count verified: a cabin with an Active Hold does not appear in CSA-002 search results
- [ ] Hold expiry job verified: Active Holds older than 5 minutes are marked Cancelled within one job interval
- [ ] Total price stored at confirmation matches what was shown on the Cabin Detail Page for the same dates (EC-006)
- [ ] Review checklist (`skills/review-checklist.md`) completed per unit
- [ ] Prompt logs written to `prompts/` for each AI-assisted generation
- [ ] Backlog statuses updated as units move
- [ ] Retro written in `ops/operate/retros/`

## Risks / Watch Items

- **EC-012 atomic transaction (BKF-002)** — `SELECT FOR UPDATE` on the hold row is the critical correctness gate; test with concurrent requests to ensure only one Booking is created per Hold. This is the highest-risk unit in the bolt.
- **EC-011 race condition (BKF-001)** — cancelling the previous Hold and inserting the new one must be in a single transaction; otherwise a concurrent request can see both Holds as Active briefly.
- **CSA-002 patch (BKF-001)** — updating the availability query to exclude Active Holds touches already-Done code; regression risk on existing CSA tests. Run the full CSA test suite after this patch.
- **`GET /api/cabins/{id}` endpoint (BKF-005)** — bundled into BKF-005 scope; it is a small public endpoint but if it grows (e.g. needs real-time availability indicator), promote it to its own unit before building.
- **`cabins.host_id` migration (BKF-009)** — stretch unit requires a schema change on an existing table; validate the migration is additive and does not break existing cabin tests before running.
- **Hold expiry vs countdown sync (BKF-006)** — the frontend countdown uses the server's `expiresAt`; the backend job runs every 5 minutes. The API must re-validate Hold status at Confirm time (EC-002), not trust the countdown.
- **Big bolt risk (accepted)** — 8 core units across backend and frontend is significant scope; the parallel tracks and the stretch designation for BKF-009 mitigate. If mid-bolt estimates slip, drop BKF-009 cleanly.

## Out of Scope

- Stripe SDK integration and webhook handling (separate intent)
- Refund processing and cancellation fees (separate Cancellation Policy intent)
- Push and email notifications on booking events (separate Notifications intent)
- Booking modification (date change after confirmation)
- Host ability to reject or cancel a Guest's Booking
- Multi-cabin bookings
- Review and rating submission post-stay
- CSA-006 Map View (still deferred from BOLT-001)

---

## Outcome
<!-- Filled in at bolt close. -->
**Completed:** —
**Units done:** — of 9
**Retro:** —
