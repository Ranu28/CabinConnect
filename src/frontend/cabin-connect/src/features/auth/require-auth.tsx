import { useEffect } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from './use-auth'

interface Props {
  children: React.ReactNode
}

export function RequireAuth({ children }: Props) {
  const { user, loading } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  useEffect(() => {
    if (!loading && !user) {
      const returnUrl = encodeURIComponent(location.pathname + location.search)
      navigate(`/login?returnUrl=${returnUrl}`, { replace: true })
    }
  }, [user, loading, navigate, location])

  if (loading) return <p>Loading…</p>
  if (!user) return null
  return <>{children}</>
}
