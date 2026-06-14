-- Migration: 002_bookings_add_currency
-- Adds currency column to bookings so the confirmed price is fully self-contained.
-- hold_id and payment_intent_id were added in migration 001.

alter table bookings add column if not exists currency text not null default 'USD';
