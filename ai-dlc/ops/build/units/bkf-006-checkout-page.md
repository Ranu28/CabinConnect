# Unit: Checkout Page

**ID:** BKF-006
**Status:** Ready
**Intent:** [Booking Flow](../../inception/intents/2026-06-14-booking-flow.md)
**Layer:** Frontend
**Priority:** High
**Date:** 2026-06-14

---

## Purpose

A page that shows the Guest their Hold details (cabin, dates, price, expiry countdown) and allows them to confirm the Booking. Handles Hold expiry gracefully.

## Acceptance Criteria

**AC-1 — Hold details displayed:**
- **Given** a valid `holdId` in the URL (`/checkout?holdId=…`)
- **When** the page loads
- **Then** cabin name, check-in/check-out dates, number of nights, total price, and Hold expiry countdown are displayed

**AC-2 — Countdown reflects server expiry time:**
- **Given** the Hold's `expiresAt` from the API response
- **When** the countdown renders
- **Then** it counts down to `expiresAt` in real time (MM:SS format); it does not reset on page refresh

**AC-3 — "Confirm Booking" calls the API and navigates on success:**
- **Given** the Hold is still Active
- **When** "Confirm Booking" is clicked
- **Then** `POST /api/bookings` is called with the `holdId`; on 201 the Guest is navigated to `/booking-confirmed?bookingId=…`

**AC-4 — Hold expiry handled when countdown reaches zero:**
- **Given** the countdown has reached zero
- **Then** the "Confirm Booking" button is disabled and an "Your hold has expired" message is shown with a "Search again" link to `/`

**AC-5 — API HOLD_EXPIRED response handled:**
- **Given** the Guest clicks "Confirm Booking" and the API returns 409 `HOLD_EXPIRED`
- **Then** the same expired state (AC-4) is shown

**AC-6 — Auth guard:**
- **Given** an unauthenticated user navigates to `/checkout`
- **Then** they are redirected to login with a `returnUrl`

**AC-7 — Invalid Hold ID handled:**
- **Given** a `holdId` that does not exist or belongs to a different Guest
- **When** the page loads
- **Then** the user is redirected to `/` with an error toast ("Hold not found")

## Dependencies

- BKF-001 — Hold must exist and be Active
- BKF-002 — "Confirm Booking" calls Confirm Booking API
- BKF-005 — Navigated to from Cabin Detail Page after successful Hold creation
