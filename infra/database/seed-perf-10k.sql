-- CabinConnect — AC-15 performance seed
-- Inserts 10,000 published cabins, ~2,000 seasonal rates, ~1,000 blackout dates.
-- Bookings are omitted: guest_id references auth.users and cannot be seeded
-- without a real Supabase auth identity. The availability query still exercises
-- the full join path; the bookings sub-query will return no rows (empty set is valid).
--
-- Run in: Supabase → SQL Editor → New query
-- Safe to re-run: DELETE cleans the perf rows first (identified by name prefix).

-- ── Clean previous perf run ───────────────────────────────────────────────
delete from blackout_dates
where cabin_id in (select id from cabins where name like 'PerfCabin-%');

delete from seasonal_rates
where cabin_id in (select id from cabins where name like 'PerfCabin-%');

delete from cabins where name like 'PerfCabin-%';

-- ── 10,000 published cabins ───────────────────────────────────────────────
insert into cabins (name, description, max_guests, base_rate, currency, amenities, is_published)
select
    'PerfCabin-' || n,
    'Auto-generated cabin for AC-15 performance verification.',
    (n % 8) + 1,                                     -- max_guests 1–8
    50.00 + (n % 450),                               -- base_rate $50–$499
    'USD',
    case (n % 4)
        when 0 then array['wifi', 'parking']
        when 1 then array['wifi', 'fireplace', 'hot-tub']
        when 2 then array['parking', 'pet-friendly']
        else        array['wifi', 'kayak', 'fireplace', 'parking']
    end,
    true
from generate_series(1, 10000) as n;

-- ── ~2,000 seasonal rates (every 5th cabin) ───────────────────────────────
insert into seasonal_rates (cabin_id, name, start_date, end_date, rate)
select
    c.id,
    'Summer Peak',
    '2026-06-01',
    '2026-08-31',
    c.base_rate * 1.30
from cabins c
where c.name like 'PerfCabin-%'
  and (substring(c.name from 11)::int % 5) = 0;

-- ── ~1,000 blackout dates (every 10th cabin, staggered windows) ───────────
insert into blackout_dates (cabin_id, start_date, end_date, reason)
select
    c.id,
    ('2026-07-01'::date + ((substring(c.name from 11)::int % 30) || ' days')::interval)::date,
    ('2026-07-08'::date + ((substring(c.name from 11)::int % 30) || ' days')::interval)::date,
    'Maintenance'
from cabins c
where c.name like 'PerfCabin-%'
  and (substring(c.name from 11)::int % 10) = 0;

-- ── Verify counts ─────────────────────────────────────────────────────────
select
    (select count(*) from cabins        where name like 'PerfCabin-%') as perf_cabins,
    (select count(*) from seasonal_rates where cabin_id in
        (select id from cabins where name like 'PerfCabin-%'))          as perf_rates,
    (select count(*) from blackout_dates where cabin_id in
        (select id from cabins where name like 'PerfCabin-%'))          as perf_blackouts;
