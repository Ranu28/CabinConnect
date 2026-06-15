import { supabase } from './supabase'

/** Fetch wrapper that injects a Supabase Bearer token when the user is signed in. */
export async function apiFetch(path: string, options?: RequestInit): Promise<Response> {
  const { data: { session } } = await supabase.auth.getSession()
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options?.headers as Record<string, string> | undefined),
  }
  if (session?.access_token) {
    headers['Authorization'] = `Bearer ${session.access_token}`
  }
  return fetch(path, { ...options, headers })
}
