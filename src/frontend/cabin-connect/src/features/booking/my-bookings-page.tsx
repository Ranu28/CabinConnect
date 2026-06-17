import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { apiFetch } from '../../lib/api-client'
import { RequireAuth } from '../auth/require-auth'
import type { GuestBookingItem } from './booking-types'

const CANCELLABLE_STATUSES = new Set(['Confirmed', 'Pending'])

function statusBadgeClass(status: string): string {
  const map: Record<string, string> = {
    Confirmed: 'badge-confirmed',
    Pending:   'badge-pending',
    Cancelled: 'badge-cancelled',
    Completed: 'badge-completed',
    NoShow:    'badge-noshow',
  }
  return `badge ${map[status] ?? 'badge-cancelled'}`
}

function MyBookingsContent() {
  const [items, setItems]       = useState<GuestBookingItem[]>([])
  const [loading, setLoading]   = useState(true)
  const [error, setError]       = useState<string | null>(null)
  const [cancelling, setCancelling] = useState<string | null>(null)

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
      if (!res.ok) { alert(json.error?.message ?? 'Could not cancel this booking.'); return }
      setItems(prev => prev.map(b =>
        b.bookingId === bookingId ? { ...b, status: 'Cancelled' } : b
      ))
    } catch {
      alert('Network error. Please try again.')
    } finally {
      setCancelling(null)
    }
  }, [])

  if (loading) return <p role="status" className="text-muted">Loading your bookings…</p>
  if (error)   return <p role="alert" className="form-error">{error}</p>

  if (items.length === 0) {
    return (
      <div style={{ textAlign: 'center', padding: '3rem 0', color: 'var(--text-muted)' }}>
        <p style={{ marginBottom: '1rem' }}>No bookings yet.</p>
        <Link to="/" className="btn btn-primary">Search cabins</Link>
      </div>
    )
  }

  return (
    <ul className="booking-list" aria-label="My bookings">
      {items.map(b => {
        const fmt    = new Intl.NumberFormat('en-US', { style: 'currency', currency: b.currency })
        const nights = Math.round(
          (new Date(b.checkOut).getTime() - new Date(b.checkIn).getTime()) / 86_400_000
        )
        return (
          <li key={b.bookingId} className="booking-item">
            <div className="booking-item__header">
              <span className="booking-item__name">{b.cabinName}</span>
              <span className={statusBadgeClass(b.status)} aria-label={`Status: ${b.status}`}>
                {b.status}
              </span>
            </div>
            <p className="booking-item__meta">
              {b.checkIn} → {b.checkOut} · {nights} {nights === 1 ? 'night' : 'nights'}
            </p>
            <p className="booking-item__price">{fmt.format(b.totalPrice)}</p>
            {CANCELLABLE_STATUSES.has(b.status) && (
              <div className="booking-item__actions">
                <button
                  className="btn btn-danger"
                  onClick={() => handleCancel(b.bookingId)}
                  disabled={cancelling === b.bookingId}
                >
                  {cancelling === b.bookingId ? 'Cancelling…' : 'Cancel booking'}
                </button>
              </div>
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
      <div className="page">
        <div className="page-header">
          <Link to="/" className="back-link">← Search</Link>
          <h1>My Bookings</h1>
        </div>
        <MyBookingsContent />
      </div>
    </RequireAuth>
  )
}
