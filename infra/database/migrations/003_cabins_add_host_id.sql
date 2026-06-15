-- BKF-009: Add host_id to cabins so Hosts can be associated with their Cabins.
-- host_id is nullable to allow existing rows to be back-filled before being made NOT NULL.

alter table cabins add column if not exists host_id uuid references auth.users (id);

-- RLS: Hosts may only update/delete their own Cabin rows.
-- Public read of published cabins (used by search) is unchanged.
-- DROP + CREATE for idempotency (CREATE POLICY IF NOT EXISTS requires PG 17).
drop policy if exists "cabins_host_update" on cabins;
create policy "cabins_host_update"
    on cabins for update
    using (auth.uid() = host_id);

drop policy if exists "cabins_host_delete" on cabins;
create policy "cabins_host_delete"
    on cabins for delete
    using (auth.uid() = host_id);

-- Index for the host bookings query join.
create index if not exists idx_cabins_host_id on cabins (host_id);
