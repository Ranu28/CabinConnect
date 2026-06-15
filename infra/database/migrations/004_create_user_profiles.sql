-- User profiles: one row per auth.users entry.
-- role is set at registration (guest/host) or granted by an Admin (admin).
-- Admin role cannot be set via the API — must be updated directly in the DB.

create table if not exists user_profiles (
    id           uuid        primary key references auth.users (id) on delete cascade,
    role         text        not null default 'guest'
                             check (role in ('guest', 'host', 'admin')),
    display_name text        not null,
    created_at   timestamptz not null default now()
);

alter table user_profiles enable row level security;

-- Users can read their own profile only.
-- DROP + CREATE is used for idempotency; CREATE POLICY IF NOT EXISTS requires PG 17.
drop policy if exists "user_profiles_self_select" on user_profiles;
create policy "user_profiles_self_select"
    on user_profiles for select
    using (auth.uid() = id);

-- Insert/update handled by the .NET API (service-role connection bypasses RLS).

create index if not exists idx_user_profiles_role on user_profiles (role);
