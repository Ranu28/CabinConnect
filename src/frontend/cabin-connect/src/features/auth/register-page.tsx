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

    // Step 1: create Supabase Auth user
    const { data, error: signUpError } = await supabase.auth.signUp({ email, password })
    if (signUpError) {
      setError(signUpError.message)
      setLoading(false)
      return
    }

    // Supabase may require email confirmation before a session is returned.
    if (!data.session) {
      setInfo('Check your email to confirm your account, then sign in.')
      setLoading(false)
      return
    }

    // Step 2: create the profile via the API (uses the session token from sign-up).
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
    <main style={{ maxWidth: 440, margin: '4rem auto', padding: '0 1rem' }}>
      <h1>Create your CabinConnect account</h1>
      <form onSubmit={handleSubmit}>
        <div style={{ marginBottom: '1rem' }}>
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
            style={{ display: 'block', width: '100%', marginTop: '0.25rem' }}
          />
        </div>

        <div style={{ marginBottom: '1rem' }}>
          <label htmlFor="email">Email</label>
          <input
            id="email"
            type="email"
            value={email}
            onChange={e => setEmail(e.target.value)}
            required
            autoComplete="email"
            style={{ display: 'block', width: '100%', marginTop: '0.25rem' }}
          />
        </div>

        <div style={{ marginBottom: '1rem' }}>
          <label htmlFor="password">Password</label>
          <input
            id="password"
            type="password"
            value={password}
            onChange={e => setPassword(e.target.value)}
            required
            minLength={8}
            autoComplete="new-password"
            style={{ display: 'block', width: '100%', marginTop: '0.25rem' }}
          />
        </div>

        <fieldset style={{ marginBottom: '1.25rem', border: '1px solid #ccc', padding: '0.75rem 1rem' }}>
          <legend>I want to…</legend>
          <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
            <input
              type="radio"
              name="role"
              value="guest"
              checked={role === 'guest'}
              onChange={() => setRole('guest')}
            />
            Book cabins as a Guest
          </label>
          <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <input
              type="radio"
              name="role"
              value="host"
              checked={role === 'host'}
              onChange={() => setRole('host')}
            />
            List and manage cabins as a Host
          </label>
        </fieldset>

        {error && <p role="alert" style={{ color: '#c00' }}>{error}</p>}
        {info  && <p role="status" style={{ color: '#060' }}>{info}</p>}

        <button type="submit" disabled={loading} style={{ width: '100%', padding: '0.6rem' }}>
          {loading ? 'Creating account…' : 'Create account'}
        </button>
      </form>

      <p style={{ marginTop: '1rem', textAlign: 'center' }}>
        Already have an account? <Link to="/login">Sign in</Link>
      </p>
    </main>
  )
}
