import { createClient } from '@supabase/supabase-js'

// These env vars are set in .env.local (gitignored — never commit them).
// VITE_SUPABASE_URL  = https://<project-ref>.supabase.co
// VITE_SUPABASE_ANON_KEY = your anon/public key from Project Settings → API
const supabaseUrl = import.meta.env.VITE_SUPABASE_URL as string
const supabaseAnonKey = import.meta.env.VITE_SUPABASE_ANON_KEY as string

if (!supabaseUrl || !supabaseAnonKey) {
  throw new Error(
    'Missing VITE_SUPABASE_URL or VITE_SUPABASE_ANON_KEY. ' +
    'Copy .env.local.example to .env.local and fill in your project values.',
  )
}

// Per architecture rules (CLAUDE.md):
// - Use this client for auth tokens and real-time subscriptions ONLY.
// - All data mutations go through the .NET API — never call Supabase PostgREST directly.
export const supabase = createClient(supabaseUrl, supabaseAnonKey)
