-- CabinConnect — Supabase / PostgreSQL schema
-- Run this in the Supabase SQL Editor (Project → SQL Editor → New query)
-- All tables use snake_case columns to match the repository queries.

-- ============================================================
-- Tables
-- ============================================================

create table if not exists cabins (
    id          uuid        primary key default gen_random_uuid(),
    name        text        not null,
    description text        not null,
    image_url   text,
    max_guests  integer     not null check (max_guests >= 1),
    base_rate   numeric(10, 2) not null check (base_rate >= 0),
    currency    text        not null default 'USD',
    amenities   text[]      not null default '{}',
    location_lat double precision,
    location_lng double precision,
    is_published boolean    not null default false,
    created_at  timestamptz not null default now(),
    updated_at  timestamptz not null default now()
);

create table if not exists seasonal_rates (
    id          uuid        primary key default gen_random_uuid(),
    cabin_id    uuid        not null references cabins (id) on delete cascade,
    name        text        not null,
    start_date  date        not null,
    end_date    date        not null,
    rate        numeric(10, 2) not null check (rate >= 0),
    created_at  timestamptz not null default now(),
    constraint seasonal_rate_valid_dates check (end_date > start_date)
);

create table if not exists bookings (
    id          uuid        primary key default gen_random_uuid(),
    cabin_id    uuid        not null references cabins (id),
    guest_id    uuid        not null references auth.users (id),
    check_in    date        not null,
    check_out   date        not null,
    -- Domain statuses: Pending | Confirmed | Cancelled | Completed | NoShow
    status      text        not null default 'Pending',
    total_price numeric(10, 2) not null,
    created_at  timestamptz not null default now(),
    updated_at  timestamptz not null default now(),
    constraint booking_valid_dates check (check_out > check_in),
    constraint booking_valid_status check (
        status in ('Pending', 'Confirmed', 'Cancelled', 'Completed', 'NoShow')
    )
);

create table if not exists blackout_dates (
    id          uuid        primary key default gen_random_uuid(),
    cabin_id    uuid        not null references cabins (id) on delete cascade,
    start_date  date        not null,
    end_date    date        not null,
    reason      text,
    created_at  timestamptz not null default now(),
    constraint blackout_valid_dates check (end_date > start_date)
);

-- ============================================================
-- Indexes for availability queries (EC-004, AC-15 performance)
-- ============================================================

-- Partial index: only published cabins — used by the search query
create index if not exists idx_cabins_published
    on cabins (id)
    where is_published = true;

-- Booking availability overlap check: (cabin_id, date range, status)
create index if not exists idx_bookings_availability
    on bookings (cabin_id, check_in, check_out, status);

-- Seasonal rate lookup by cabin
create index if not exists idx_seasonal_rates_cabin
    on seasonal_rates (cabin_id, start_date, end_date);

-- Blackout date overlap check
create index if not exists idx_blackout_dates_cabin
    on blackout_dates (cabin_id, start_date, end_date);

-- ============================================================
-- Row Level Security
-- All tables must have RLS enabled (CLAUDE.md rule)
-- ============================================================

alter table cabins        enable row level security;
alter table seasonal_rates enable row level security;
alter table bookings      enable row level security;
alter table blackout_dates enable row level security;

-- cabins: any anonymous user may read published cabins (powers the search)
drop policy if exists "cabins_public_read" on cabins;
create policy "cabins_public_read" on cabins
    for select
    using (is_published = true);

-- seasonal_rates: readable for any published cabin
drop policy if exists "seasonal_rates_public_read" on seasonal_rates;
create policy "seasonal_rates_public_read" on seasonal_rates
    for select
    using (
        exists (
            select 1 from cabins
            where cabins.id = seasonal_rates.cabin_id
              and cabins.is_published = true
        )
    );

-- blackout_dates: readable for any published cabin
drop policy if exists "blackout_dates_public_read" on blackout_dates;
create policy "blackout_dates_public_read" on blackout_dates
    for select
    using (
        exists (
            select 1 from cabins
            where cabins.id = blackout_dates.cabin_id
              and cabins.is_published = true
        )
    );

-- bookings: guests can manage their own bookings only (EC-007)
drop policy if exists "bookings_guest_select" on bookings;
create policy "bookings_guest_select" on bookings
    for select
    using (auth.uid() = guest_id);

drop policy if exists "bookings_guest_insert" on bookings;
create policy "bookings_guest_insert" on bookings
    for insert
    with check (auth.uid() = guest_id);

drop policy if exists "bookings_guest_update" on bookings;
create policy "bookings_guest_update" on bookings
    for update
    using (auth.uid() = guest_id);
