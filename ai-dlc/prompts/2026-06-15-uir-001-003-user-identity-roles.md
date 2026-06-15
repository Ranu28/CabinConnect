# Prompt Log: UIR-001–003 — User Identity & Roles

**Date:** 2026-06-15
**Units:** UIR-001 (User Profiles DB + Role System), UIR-002 (Registration Page), UIR-003 (Navigation Bar)
**Model:** claude-sonnet-4-6

---

## Context

After completing BOLT-002 (Booking Flow), the user identified that no real auth flow existed: the frontend
opened directly to the cabin search with no sign-up, no role system, and no visible navigation. Host
endpoints had no role enforcement either — any authenticated user could call them.

Design decisions (user-confirmed):
- Users choose their role at registration: **Guest** or **Host** (radio button)
- **Admin** role is needed now (for future Host promotion) but is not self-assignable — granted via DB only
- No Admin UI in this batch; Admin users are inserted directly into the DB

---

## Prompt (abbreviated intent)

> Create the auth flows, user registration flows, all integrations related to Auth for Hosts, Admins, and
> Guests. Ensure the backend and DB are ready for role-based access.

## Output

### DB
- `infra/database/migrations/004_create_user_profiles.sql`
  - `user_profiles (id, role, display_name, created_at)` — FK to `auth.users`
  - RLS: users read their own row only
  - Role constraint: `check (role in ('guest', 'host', 'admin'))`

### Backend — Domain
- `CabinConnect.Domain/Users/UserProfile.cs` — record: `(Id, Role, DisplayName)`
- `CabinConnect.Domain/Users/IUserProfileRepository.cs` — `GetByIdAsync`, `CreateAsync`

### Backend — Infrastructure
- `CabinConnect.Infrastructure/Users/UserProfileRepository.cs`
  - `GetByIdAsync`: SELECT by PK
  - `CreateAsync`: `INSERT … ON CONFLICT (id) DO NOTHING`; throws `InvalidOperationException` if no row inserted (profile exists)

### Backend — API
- `CabinConnect.Api/Models/CreateProfileRequest.cs` — `(DisplayName, Role)`
- `CabinConnect.Api/Controllers/MeController.cs`
  - `GET /api/me/profile` — returns current user's role + display name; 404 if no profile
  - `POST /api/me/profile` — creates profile; only "guest" and "host" accepted (not "admin"); 409 if exists
- `HostController.cs` updated — calls `IUserProfileRepository.GetByIdAsync` before returning bookings;
  returns 403 if role is not "host" or "admin"
- `Program.cs` — `AddScoped<IUserProfileRepository, UserProfileRepository>()`

### Frontend
- `profile-types.ts` — `UserRole` type + `UserProfile` interface
- `use-profile.ts` — hook: fetches `GET /api/me/profile` when user is signed in; clears on sign-out
- `register-page.tsx` — two-step: `supabase.auth.signUp()` → `POST /api/me/profile`; handles
  email-confirmation-pending case; redirects to `/host/bookings` for hosts, `/` for guests
- `nav-bar.tsx` — role-aware links: Guests see "My Bookings"; Hosts see "Host Bookings"; Admins see both;
  unauthenticated shows "Sign in" + "Register"; sign-out calls `supabase.auth.signOut()`
- `App.tsx` — added `<NavBar />` above `<Routes>`, added `/register` route
- `login-page.tsx` — added "No account? Create one" link to `/register`

### Tests
- `HostControllerTests.cs` — `BuildClient` updated to also mock `IUserProfileRepository` returning a
  "host" profile for the test user (`FakeAuthHandler.GuestId`)

## Edge Cases Checked
- EC-007: N/A (profiles are user-scoped; no cross-user access)
- Admin role not self-assignable (backend rejects via `_allowedSelfRoles`)
- Email confirmation pending: registration page shows informational message and stops (no crash)
- Profile already exists: `POST /api/me/profile` returns 409 (idempotent-friendly)
- Host endpoint 403 (not 401) when role is wrong but authentication is valid

## Results
- 51/51 tests passing (24 domain + 27 API)
- 24/24 frontend Vitest tests passing
- TypeScript clean (`tsc --noEmit` exits 0)
