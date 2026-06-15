import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { apiFetch } from '../../lib/api-client'
import { RequireAuth } from '../auth/require-auth'
import type { BookingStatus } from '../booking/booking-types'

interface HostBookingItem {
  bookingId:  string
  cabinId:    string
  cabinName:  string
  guestRef:   string
  checkIn:    string
  checkOut:   string
  totalPrice: number
  currency:   string
  status:     BookingStatus
}

const STATUS_OPTIONS: Array<{ label: string; value: string }> = [
  { label: 'All',       value: '' },
  { label: 'Pending',   value: 'Pending' },
  { label: 'Confirmed', value: 'Confirmed' },
  { label: 'Cancelled', value: 'Cancelled' },
  { label: 'Completed', value: 'Completed' },
  { label: 'NoShow',    value: 'NoShow' },
]

function HostBookingsContent() {
  const [items, setItems]         = useState<HostBookingItem[]>([])
  const [loading, setLoading]     = useState(true)
  const [error, setError]         = useState<string | null>(null)
  const [statusFilter, setStatusFilter] = useState('')

  const loadBookings = useCallback(async (status: string) => {
    setLoading(true)
    setError(null)
    try {
      const qs  = status ? `?status=${status}` : ''
      const res = await apiFetch(`/api/host/bookings${qs}`)
      const json = await res.json()
      if (!res.ok) throw new Error(json.error?.message ?? 'Failed to load bookings')
      setItems(json.data.items as HostBookingItem[])
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load bookings')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { void loadBookings(statusFilter) }, [loadBookings, statusFilter])

  // AC-7: status filter dropdown
  function handleStatusChange(e: React.ChangeEvent<HTMLSelectElement>) {
    setStatusFilter(e.target.value)
  }

  return (
    <>
      <div style={{ marginBottom: '1rem' }}>
        <label htmlFor="status-filter">Filter by status: </label>
        <select id="status-filter" value={statusFilter} onChange={handleStatusChange}>
          {STATUS_OPTIONS.map(o => (
            <option key={o.value} value={o.value}>{o.label}</option>
          ))}
        </select>
      </div>

      {loading && <p role="status">Loading bookings…</p>}
      {error   && <p role="alert" style={{ color: 'red' }}>{error}</p>}

      {/* AC-8: empty state */}
      {!loading && !error && items.length === 0 && (
        <p>No bookings yet.</p>
      )}

      {/* AC-6: booking table */}
      {!loading && !error && items.length > 0 && (
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr>
              <th style={{ textAlign: 'left', padding: '0.5rem', borderBottom: '2px solid #eee' }}>Cabin</th>
              <th style={{ textAlign: 'left', padding: '0.5rem', borderBottom: '2px solid #eee' }}>Check-in</th>
              <th style={{ textAlign: 'left', padding: '0.5rem', borderBottom: '2px solid #eee' }}>Check-out</th>
              <th style={{ textAlign: 'left', padding: '0.5rem', borderBottom: '2px solid #eee' }}>Guest ref</th>
              <th style={{ textAlign: 'right', padding: '0.5rem', borderBottom: '2px solid #eee' }}>Total</th>
              <th style={{ textAlign: 'left', padding: '0.5rem', borderBottom: '2px solid #eee' }}>Status</th>
            </tr>
          </thead>
          <tbody>
            {items.map(b => {
              const fmt = new Intl.NumberFormat('en-US', { style: 'currency', currency: b.currency })
              return (
                <tr key={b.bookingId} style={{ borderBottom: '1px solid #eee' }}>
                  <td style={{ padding: '0.5rem' }}>{b.cabinName}</td>
                  <td style={{ padding: '0.5rem' }}>{b.checkIn}</td>
                  <td style={{ padding: '0.5rem' }}>{b.checkOut}</td>
                  <td style={{ padding: '0.5rem', fontFamily: 'monospace' }}>{b.guestRef}</td>
                  <td style={{ padding: '0.5rem', textAlign: 'right' }}>{fmt.format(b.totalPrice)}</td>
                  <td style={{ padding: '0.5rem' }}>
                    <span style={{ fontWeight: 'bold' }}>{b.status}</span>
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      )}
    </>
  )
}

export function HostBookingsPage() {
  return (
    <RequireAuth>
      <main style={{ maxWidth: 1000, margin: '2rem auto', padding: '0 1rem' }}>
        <h1>Host — Bookings</h1>
        <Link to="/">← Back to search</Link>
        <HostBookingsContent />
      </main>
    </RequireAuth>
  )
}
