-- CabinConnect — sample data for local development / testing
-- Run AFTER schema.sql in the Supabase SQL Editor.

insert into cabins (id, name, description, image_url, max_guests, base_rate, currency, amenities, is_published)
values
    (
        'a1000000-0000-0000-0000-000000000001',
        'Pine Ridge Cabin',
        'A cosy cabin nestled in the pines with a wood-burning fireplace and mountain views.',
        'https://images.unsplash.com/photo-1510798831971-661eb04b3739?w=800',
        4,
        150.00,
        'USD',
        array['wifi', 'fireplace', 'hot-tub', 'parking'],
        true
    ),
    (
        'a1000000-0000-0000-0000-000000000002',
        'Lakeview Retreat',
        'Wake up to stunning lake views from this spacious cabin with a private dock.',
        'https://images.unsplash.com/photo-1449158743715-0a90ebb6d2d8?w=800',
        6,
        220.00,
        'USD',
        array['wifi', 'fireplace', 'kayak', 'parking', 'pet-friendly'],
        true
    ),
    (
        'a1000000-0000-0000-0000-000000000003',
        'Forest Hideaway',
        'Disconnect from the world in this secluded forest cabin — no neighbours for miles.',
        'https://images.unsplash.com/photo-1416526788662-f6f9b019d2c4?w=800',
        2,
        95.00,
        'USD',
        array['fireplace', 'parking'],
        true
    );

-- Seasonal rates: summer premium on Pine Ridge
insert into seasonal_rates (cabin_id, name, start_date, end_date, rate)
values
    (
        'a1000000-0000-0000-0000-000000000001',
        'Summer Peak',
        '2026-06-21',
        '2026-09-01',
        210.00
    ),
    (
        'a1000000-0000-0000-0000-000000000002',
        'Summer Peak',
        '2026-06-21',
        '2026-09-01',
        300.00
    );

-- A blackout date on Lakeview for maintenance
insert into blackout_dates (cabin_id, start_date, end_date, reason)
values
    (
        'a1000000-0000-0000-0000-000000000002',
        '2026-07-01',
        '2026-07-05',
        'Annual maintenance'
    );
