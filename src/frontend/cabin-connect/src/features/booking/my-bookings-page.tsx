import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { apiFetch } from '../../lib/api-client'
import { RequireAuth } from '../auth/require-auth'
import type { GuestBookingItem } from './booking-types'

const CANCELLABLE_STATUSES = new Set(['Confirmed', 'Pending'])

function MyBookingsContent() {
  const [items, setItems]         = useState<GuestBookingItem[]>([])
  const [loading, setLoading]     = useState(true)
  const [error, setError]         = useState<string | null>(null)
  const [cancelling, setCancelling] = useState<string | null>(null) // bookingId being cancelled

  const loadBookings = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res  = await apiFetch('/api/bookings')
      const json = await res.json()
      if (!res.ok) throw new Error(json.error?.message ?? 'Failed to load bookings')
      setItems(json.data.items as GuestBookingItem[])
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load bookings')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { void loadBookings() }, [loadBookings])

  const handleCancel = useCallback(async (bookingId: string) => {
    if (!window.confirm('Are you sure you want to cancel this booking?')) return
    setCancelling(bookingId)
    try {
      const res  = await apiFetch(`/api/bookings/${bookingId}/cancel`, { method: 'POST' })
      const json = await res.json()
      if (!res.ok) {
        alert(json.error?.message ?? 'Could not cancel this booking.')
        return
      }
      // AC-6: refresh list on success
      setItems(prev => prev.map(b =>
        b.bookingId === bookingId ? { ...b, status: 'Cancelled' } : b
      ))
    } catch {
      alert('Network error. Please try again.')
    } finally {
      setCancelling(null)
    }
  }, [])

  if (loading) return <p role="status">Loading your bookings…</p>
  if (error)   return <p role="alert" style={{ color: 'red' }}>{error}</p>

  // AC-7: empty state
  if (items.length === 0) {
    return (
      <p>
        No bookings yet.{' '}
        <Link to="/">Search cabins</Link>
      </p>
    )
  }

  return (
    <ul aria-label="My bookings" style={{ listStyle: 'none', padding: 0 }}>
      {items.map(b => {
        const fmt    = new Intl.NumberFormat('en-US', { style: 'currency', currency: b.currency })
        const nights = Math.round(
          (new Date(b.checkOut).getTime() - new Date(b.checkIn).getTime()) / 86_400_000
        )
        return (
          <li key={b.bookingId} style={{ borderBottom: '1px solid #eee', padding: '1rem 0' }}>
            {/* AC-5: cabin name, dates, price, status */}
            <h2 style={{ margin: 0 }}>{b.cabinName}</h2>
            <p>
              {b.checkIn} → {b.checkOut} ({nights} {nights === 1 ? 'night' : 'nights'})
            </p>
            <p>{fmt.format(b.totalPrice)}</p>
            <p>
              <span aria-label={`Status: ${b.status}`}
                    style={{ fontWeight: 'bold', color: b.status === 'Cancelled' ? '#888' : 'inherit' }}>
                {b.status}
              </span>
            </p>
            {/* AC-6: cancel button for Confirmed/Pending only */}
            {CANCELLABLE_STATUSES.has(b.status) && (
              <button
                onClick={() => handleCancel(b.bookingId)}
                disabled={cancelling === b.bookingId}
              >
                {cancelling === b.bookingId ? 'Cancelling…' : 'Cancel'}
              </button>
            )}
          </li>
        )
      })}
    </ul>
  )
}

export function MyBookingsPage() {
  return (
    <RequireAuth>
      <main style={{ maxWidth: 800, margin: '2rem auto', padding: '0 1rem' }}>
        <h1>My Bookings</h1>
        <Link to="/">← Back to search</Link>
        <MyBookingsContent />
      </main>
    </RequireAuth>
  )
}
