import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { supabase } from '../../lib/supabase'
import { apiFetch } from '../../lib/api-client'

export function RegisterPage() {
  const [displayName, setDisplayName] = useState('')
  const [email, setEmail]             = useState('')
  const [password, setPassword]       = useState('')
  const [role, setRole]               = useState<'guest' | 'host'>('guest')
  const [error, setError]             = useState<string | null>(null)
  const [info, setInfo]               = useState<string | null>(null)
  const [loading, setLoading]         = useState(false)
  const navigate                      = useNavigate()

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setInfo(null)
    setLoading(true)

    const { data, error: signUpError } = await supabase.auth.signUp({ email, password })
    if (signUpError) {
      setError(signUpError.message)
      setLoading(false)
      return
    }

    if (!data.session) {
      setInfo('Check your email to confirm your account, then sign in.')
      setLoading(false)
      return
    }

    const res = await apiFetch('/api/me/profile', {
      method: 'POST',
      body: JSON.stringify({ displayName: displayName.trim(), role }),
    })

    if (!res.ok) {
      const json = await res.json().catch(() => null)
      setError(json?.error?.message ?? 'Failed to create profile. Please try again.')
      setLoading(false)
      return
    }

    setLoading(false)
    navigate(role === 'host' ? '/host/bookings' : '/', { replace: true })
  }

  return (
    <div className="auth-page">
      <div className="auth-card" style={{ maxWidth: 460 }}>
        <Link to="/" className="auth-logo">CabinConnect</Link>

        <h2 style={{ textAlign: 'center', marginBottom: '1.5rem' }}>Create your account</h2>

        <form onSubmit={handleSubmit}>
          <div className="form-field">
            <label htmlFor="displayName">Display name</label>
            <input
              id="displayName"
              type="text"
              value={displayName}
              onChange={e => setDisplayName(e.target.value)}
              required
              minLength={1}
              maxLength={100}
              autoComplete="name"
              placeholder="Jane Smith"
            />
          </div>

          <div className="form-field">
            <label htmlFor="email">Email</label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              required
              autoComplete="email"
              placeholder="you@example.com"
            />
          </div>

          <div className="form-field">
            <label htmlFor="password">Password</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              required
              minLength={8}
              autoComplete="new-password"
              placeholder="Min. 8 characters"
            />
          </div>

          <fieldset style={{ marginBottom: '1.25rem' }}>
            <legend>I want to…</legend>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem', marginTop: '0.5rem' }}>
              <label style={{ display: 'flex', alignItems: 'center', gap: '0.625rem', textTransform: 'none', letterSpacing: 0, fontSize: '0.9375rem', fontWeight: 500, cursor: 'pointer', marginBottom: 0 }}>
                <input
                  type="radio"
                  name="role"
                  value="guest"
                  checked={role === 'guest'}
                  onChange={() => setRole('guest')}
                />
                Book cabins as a Guest
              </label>
              <label style={{ display: 'flex', alignItems: 'center', gap: '0.625rem', textTransform: 'none', letterSpacing: 0, fontSize: '0.9375rem', fontWeight: 500, cursor: 'pointer', marginBottom: 0 }}>
                <input
                  type="radio"
                  name="role"
                  value="host"
                  checked={role === 'host'}
                  onChange={() => setRole('host')}
                />
                List and manage cabins as a Host
              </label>
            </div>
          </fieldset>

          {error && <p className="form-error" role="alert">{error}</p>}
          {info  && <p className="form-info"  role="status">{info}</p>}

          <button
            type="submit"
            className="btn btn-primary"
            disabled={loading}
            style={{ width: '100%' }}
          >
            {loading ? 'Creating account…' : 'Create account'}
          </button>
        </form>

        <p className="auth-footer">
          Already have an account?{' '}
          <Link to="/login">Sign in</Link>
        </p>
      </div>
    </div>
  )
}
