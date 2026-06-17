import { Link, useNavigate } from 'react-router-dom'
import { supabase } from '../lib/supabase'
import { useAuth } from '../features/auth/use-auth'
import { useProfile } from '../features/auth/use-profile'

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
    <nav className="navbar">
      <Link to="/" className="navbar__logo">CabinConnect</Link>

      {loading && <span className="navbar__user">Loading…</span>}

      {!loading && user && profile && (
        <>
          {(profile.role === 'guest' || profile.role === 'admin') && (
            <Link to="/my-bookings" className="navbar__link">My Bookings</Link>
          )}
          {(profile.role === 'host' || profile.role === 'admin') && (
            <Link to="/host/bookings" className="navbar__link">Host Bookings</Link>
          )}
          <span className="navbar__role">{profile.role}</span>
          <span className="navbar__user">{profile.displayName}</span>
          <button className="navbar__signout" onClick={handleSignOut}>
            Sign out
          </button>
        </>
      )}

      {!loading && !user && (
        <>
          <Link to="/login" className="navbar__link">Sign in</Link>
          <Link to="/register" className="btn btn-primary" style={{ fontSize: '0.875rem', padding: '0.4375rem 1rem' }}>
            Register
          </Link>
        </>
      )}
    </nav>
  )
}
