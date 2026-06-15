import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { supabase } from '../../lib/supabase'

export function LoginPage() {
  const [email, setEmail]         = useState('')
  const [password, setPassword]   = useState('')
  const [error, setError]         = useState<string | null>(null)
  const [loading, setLoading]     = useState(false)
  const navigate                  = useNavigate()
  const [searchParams]            = useSearchParams()
  const returnUrl                 = searchParams.get('returnUrl') ?? '/'

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setLoading(true)
    const { error: authError } = await supabase.auth.signInWithPassword({ email, password })
    setLoading(false)
    if (authError) {
      setError(authError.message)
    } else {
      navigate(returnUrl, { replace: true })
    }
  }

  return (
    <main style={{ maxWidth: 400, margin: '4rem auto', padding: '0 1rem' }}>
      <h1>Sign in to CabinConnect</h1>
      <form onSubmit={handleSubmit}>
        <div>
          <label htmlFor="email">Email</label>
          <input
            id="email"
            type="email"
            value={email}
            onChange={e => setEmail(e.target.value)}
            required
            autoComplete="email"
          />
        </div>
        <div>
          <label htmlFor="password">Password</label>
          <input
            id="password"
            type="password"
            value={password}
            onChange={e => setPassword(e.target.value)}
            required
            autoComplete="current-password"
          />
        </div>
        {error && <p role="alert" style={{ color: 'red' }}>{error}</p>}
        <button type="submit" disabled={loading}>
          {loading ? 'Signing in…' : 'Sign in'}
        </button>
      </form>

      <p style={{ marginTop: '1rem', textAlign: 'center' }}>
        No account? <Link to="/register">Create one</Link>
      </p>
    </main>
  )
}
