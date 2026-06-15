import { Link, useNavigate } from 'react-router-dom'
import { supabase } from '../lib/supabase'
import { useAuth } from '../features/auth/use-auth'
import { useProfile } from '../features/auth/use-profile'

const NAV: React.CSSProperties = {
  display: 'flex',
  alignItems: 'center',
  gap: '1.25rem',
  padding: '0.75rem 1.5rem',
  borderBottom: '1px solid #ddd',
  background: '#fff',
}

const LOGO: React.CSSProperties = {
  fontWeight: 700,
  fontSize: '1.1rem',
  textDecoration: 'none',
  color: '#1a1a1a',
  marginRight: 'auto',
}

const ROLE_BADGE: React.CSSProperties = {
  fontSize: '0.75rem',
  padding: '0.15rem 0.5rem',
  borderRadius: 4,
  background: '#f0f0f0',
  color: '#555',
  textTransform: 'capitalize',
}

export function NavBar() {
  const { user, loading: authLoading } = useAuth()
  const { profile, loading: profileLoading } = useProfile()
  const navigate = useNavigate()

  async function handleSignOut() {
    await supabase.auth.signOut()
    navigate('/', { replace: true })
  }

  const loading = authLoading || profileLoading

  return (
    <nav style={NAV}>
      <Link to="/" style={LOGO}>CabinConnect</Link>

      {loading && <span style={{ color: '#888', fontSize: '0.875rem' }}>Loading…</span>}

      {!loading && user && profile && (
        <>
          {/* Guests see My Bookings; Hosts and Admins see both */}
          {(profile.role === 'guest' || profile.role === 'admin') && (
            <Link to="/my-bookings">My Bookings</Link>
          )}
          {(profile.role === 'host' || profile.role === 'admin') && (
            <Link to="/host/bookings">Host Bookings</Link>
          )}
          <span style={ROLE_BADGE}>{profile.role}</span>
          <span style={{ fontSize: '0.875rem', color: '#555' }}>{profile.displayName}</span>
          <button
            onClick={handleSignOut}
            style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#555' }}
          >
            Sign out
          </button>
        </>
      )}

      {!loading && !user && (
        <>
          <Link to="/login">Sign in</Link>
          <Link
            to="/register"
            style={{
              padding: '0.35rem 0.85rem',
              background: '#1a1a1a',
              color: '#fff',
              borderRadius: 4,
              textDecoration: 'none',
            }}
          >
            Register
          </Link>
        </>
      )}
    </nav>
  )
}
