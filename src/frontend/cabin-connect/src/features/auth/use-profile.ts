import { useEffect, useState } from 'react'
import { apiFetch } from '../../lib/api-client'
import { useAuth } from './use-auth'
import type { UserProfile } from './profile-types'

interface ProfileState {
  profile: UserProfile | null
  loading: boolean
}

export function useProfile(): ProfileState {
  const { user } = useAuth()
  const [state, setState] = useState<ProfileState>({ profile: null, loading: true })

  useEffect(() => {
    if (!user) {
      setState({ profile: null, loading: false })
      return
    }

    setState(s => ({ ...s, loading: true }))

    apiFetch('/api/me/profile')
      .then(r => (r.ok ? r.json() : null))
      .then(json => {
        const data = json?.data ?? null
        setState({
          profile: data
            ? { id: data.id, role: data.role, displayName: data.displayName }
            : null,
          loading: false,
        })
      })
      .catch(() => setState({ profile: null, loading: false }))
  }, [user])

  return state
}
