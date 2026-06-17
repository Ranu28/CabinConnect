-- Migration: 001_create_holds
-- Creates the holds table for BKF-001 (Place Hold).
-- Run in Supabase SQL editor or via migration tooling.

create table if not exists holds (
    id         uuid        primary key default gen_random_uuid(),
    cabin_id   uuid        not null references cabins (id),
    guest_id   uuid        not null references auth.users (id),
    check_in   date        not null,
    check_out  date        not null,
    expires_at timestamptz not null,
    status     text        not null default 'Active',
    created_at timestamptz not null default now(),
    constraint hold_valid_dates  check (check_out > check_in),
    constraint hold_valid_status check (status in ('Active', 'Consumed', 'Cancelled'))
);

-- Covering index for the availability exclusion query in CSA-002 (AC-10).
create index if not exists idx_holds_availability
    on holds (cabin_id, check_in, check_out, status, expires_at);

-- Index to efficiently find a guest's existing Active Hold (EC-011 auto-cancel).
create index if not exists idx_holds_guest
    on holds (guest_id, status);

alter table holds enable row level security;

-- Guests can only read their own Holds.
drop policy if exists "holds_guest_select" on holds;
create policy "holds_guest_select"
    on holds for select
    using (auth.uid() = guest_id);

-- Guests can only insert Holds they own.
drop policy if exists "holds_guest_insert" on holds;
create policy "holds_guest_insert"
    on holds for insert
    with check (auth.uid() = guest_id);

-- Guests can only update their own Holds (needed for cancel via API).
drop policy if exists "holds_guest_update" on holds;
create policy "holds_guest_update"
    on holds for update
    using (auth.uid() = guest_id);

-- bookings table additions (add hold_id for traceability, payment_intent_id for Stripe).
alter table bookings add column if not exists hold_id          uuid references holds (id);
alter table bookings add column if not exists payment_intent_id text;
