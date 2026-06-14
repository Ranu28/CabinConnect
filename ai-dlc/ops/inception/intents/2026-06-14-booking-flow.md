# Intent: Booking Flow

**Status:** Elaborated
**Date:** 2026-06-14
**Owner:** RP

---

## What

A Guest can place a temporary Hold on an available cabin for a requested date range, complete payment checkout while the Hold is active, and receive a Confirmed Booking with the total price frozen at the moment of confirmation. Hosts can view all Bookings against their cabins and manage status transitions. Expired Holds are automatically released so the cabin returns to availability without manual intervention.

## Why

Search & Availability (CSA) ends at "here are cabins you could book." Without the Booking flow, CabinConnect has no revenue path — Guests cannot transact and Hosts cannot earn. The Hold mechanism protects the Guest's chosen cabin during the payment window while keeping the catalogue live for other Guests if the session is abandoned. Freezing the total price at confirmation (EC-006) ensures Hosts cannot change rates mid-checkout and erodes trust. This intent is the single most impactful deliverable after search.

## Success Looks Like

- A Guest viewing a search result can place a Hold on a cabin for their date range; the cabin disappears from other Guests' search results for 15 minutes.
- A Guest with an active Hold can complete payment and receive a Confirmed Booking with a total price that matches exactly what was shown in search.
- If a Guest's Hold expires before payment completes, the system returns a clear error and the cabin re-enters availability immediately.
- A Guest can cancel a Confirmed Booking and the booking status transitions correctly.
- A Host can view all Bookings for their cabins with status and guest details.
- Holds that expire without payment are automatically cancelled by a background job; no manual cleanup is required.
- The cabin availability query (CSA-002) excludes cabins with an active Hold, consistent with how it excludes Pending and Confirmed bookings.

## Assumptions

- Payment is handled by an external provider (Stripe); this intent covers the Hold → payment intent → Confirmed Booking handoff, not the Stripe SDK integration itself. A `PaymentIntentId` is stored on the Booking but Stripe webhook handling is a separate intent.
- A Guest may only hold one cabin at a time per session; placing a second Hold releases the first.
- The Hold expiry window is 15 minutes (per domain glossary), enforced by a background cleanup job — not a real-time timer.
- Hosts are identified by a `host_id` column on the `cabins` table (to be added if not present).
- Cancellation refund logic is out of scope for this intent; the status transition is all that is required here.
- Auth is required for all Booking and Hold operations; the public search endpoint remains unauthenticated.

## Open Questions

_None remaining — all resolved 2026-06-14. See Decisions below._

## Decisions

| Question | Decision (2026-06-14) |
|---|---|
| Hold expiry job frequency | Every 5 minutes. Simple to operate; acceptable trade-off — a cabin may appear unavailable for up to 5 minutes after a Hold expires. |
| Cancellation notifications | Deferred to a Notifications intent. No emails or push in this intent; status transitions are the only observable outcome. |
| Cancellation policy | Free cancellation always — no fees, no minimum notice period. A future Cancellation Policy intent will add fee tiers and due dates. |
| Host rejection | Confirmation is always final from the Host's perspective. Hosts cannot reject a Confirmed booking in this intent. |
| Booking visibility | Guest and Host see the same status set (`Pending` / `Confirmed` / `Cancelled` / `Completed` / `NoShow`). Views differ by filter (own bookings vs cabin bookings) but not by status vocabulary. |

## Out of Scope

- Stripe webhook handling and payment reconciliation (separate intent).
- Refund calculation and processing.
- Host payout flows.
- Review and rating submission post-stay.
- Push or email notifications (deferred to a Notifications intent).
- Cancellation fees and policy due dates (deferred to a Cancellation Policy intent).
- Booking modification (date change after confirmation).
- Multi-cabin bookings (one Booking = one Cabin always).
- CSA-006 Map View — still deferred.

---

## Elaboration Sessions

| Session | Date | Units Extracted |
|---|---|---|
| [Session 1](../../ops/inception/elaborations/booking-flow/2026-06-14-session-1.md) | 2026-06-14 | 9 |

## Extracted Units

| Unit | File | Status |
|---|---|---|
| BKF-001 Place Hold | [bkf-001-place-hold.md](../../ops/build/units/bkf-001-place-hold.md) | Ready |
| BKF-002 Confirm Booking | [bkf-002-confirm-booking.md](../../ops/build/units/bkf-002-confirm-booking.md) | Ready |
| BKF-003 Cancel Booking | [bkf-003-cancel-booking.md](../../ops/build/units/bkf-003-cancel-booking.md) | Ready |
| BKF-004 Hold Expiry Background Service | [bkf-004-hold-expiry-service.md](../../ops/build/units/bkf-004-hold-expiry-service.md) | Ready |
| BKF-005 Cabin Detail Page | [bkf-005-cabin-detail-page.md](../../ops/build/units/bkf-005-cabin-detail-page.md) | Ready |
| BKF-006 Checkout Page | [bkf-006-checkout-page.md](../../ops/build/units/bkf-006-checkout-page.md) | Ready |
| BKF-007 Booking Confirmation Page | [bkf-007-booking-confirmation-page.md](../../ops/build/units/bkf-007-booking-confirmation-page.md) | Ready |
| BKF-008 Guest My Bookings | [bkf-008-guest-my-bookings.md](../../ops/build/units/bkf-008-guest-my-bookings.md) | Ready |
| BKF-009 Host Booking Management | [bkf-009-host-booking-management.md](../../ops/build/units/bkf-009-host-booking-management.md) | Ready |
