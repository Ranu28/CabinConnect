# Unit: Booking Confirmation Page

**ID:** BKF-007
**Status:** Ready
**Intent:** [Booking Flow](../../inception/intents/2026-06-14-booking-flow.md)
**Layer:** Frontend
**Priority:** Medium
**Date:** 2026-06-14

---

## Purpose

A read-only success page shown after a Booking is confirmed. Displays booking reference, cabin, dates, and total price. Entry point to the Guest's booking history.

## Acceptance Criteria

**AC-1 — Booking details displayed:**
- **Given** a valid `bookingId` in the URL (`/booking-confirmed?bookingId=…`)
- **When** the page loads
- **Then** booking ID (reference), cabin name, check-in date, check-out date, number of nights, and total price are displayed

**AC-2 — Success confirmation shown:**
- **Given** the page loads successfully
- **Then** a "Your booking is confirmed!" heading is displayed prominently

**AC-3 — Link to My Bookings:**
- **Given** the confirmation page is shown
- **Then** a "View my bookings" link navigates to `/my-bookings`

**AC-4 — Link to Search:**
- **Given** the confirmation page is shown
- **Then** a "Back to search" link navigates to `/`

**AC-5 — Invalid booking ID redirects:**
- **Given** a `bookingId` that does not exist or is not owned by the Guest
- **When** the page loads
- **Then** the Guest is redirected to `/` with an error toast

**AC-6 — Auth guard:**
- **Given** an unauthenticated user navigates to this page
- **Then** they are redirected to login

## Dependencies

- BKF-002 — Booking must be Confirmed before this page can display it
- BKF-006 — Navigated to from Checkout Page after successful confirmation
