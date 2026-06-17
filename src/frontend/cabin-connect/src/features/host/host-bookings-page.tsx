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

function HostBookingsContent() {
  const [items, setItems]               = useState<HostBookingItem[]>([])
  const [loading, setLoading]           = useState(true)
  const [error, setError]               = useState<string | null>(null)
  const [statusFilter, setStatusFilter] = useState('')

  const loadBookings = useCallback(async (status: string) => {
    setLoading(true)
    setError(null)
    try {
      const qs   = status ? `?status=${status}` : ''
      const res  = await apiFetch(`/api/host/bookings${qs}`)
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

  return (
    <>
      <div className="filter-bar">
        <label htmlFor="status-filter">Filter by status:</label>
        <select
          id="status-filter"
          value={statusFilter}
          onChange={e => setStatusFilter(e.target.value)}
        >
          {STATUS_OPTIONS.map(o => (
            <option key={o.value} value={o.value}>{o.label}</option>
          ))}
        </select>
      </div>

      {loading && <p role="status" className="text-muted">Loading bookings…</p>}
      {error   && <p role="alert" className="form-error">{error}</p>}

      {!loading && !error && items.length === 0 && (
        <p className="text-muted">No bookings yet.</p>
      )}

      {!loading && !error && items.length > 0 && (
        <div className="table-wrapper">
          <table className="data-table">
            <thead>
              <tr>
                <th>Cabin</th>
                <th>Check-in</th>
                <th>Check-out</th>
                <th>Guest ref</th>
                <th className="align-right">Total</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {items.map(b => {
                const fmt = new Intl.NumberFormat('en-US', { style: 'currency', currency: b.currency })
                return (
                  <tr key={b.bookingId}>
                    <td style={{ fontWeight: 600, color: 'var(--text-h)' }}>{b.cabinName}</td>
                    <td>{b.checkIn}</td>
                    <td>{b.checkOut}</td>
                    <td style={{ fontFamily: 'var(--mono)', fontSize: '0.8125rem', color: 'var(--text-muted)' }}>
                      {b.guestRef}
                    </td>
                    <td className="align-right" style={{ fontWeight: 600 }}>
                      {fmt.format(b.totalPrice)}
                    </td>
                    <td>
                      <span className={statusBadgeClass(b.status)}>{b.status}</span>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}

export function HostBookingsPage() {
  return (
    <RequireAuth>
      <div className="page page--wide">
        <div className="page-header">
          <Link to="/" className="back-link">← Search</Link>
          <h1>Host Bookings</h1>
        </div>
        <HostBookingsContent />
      </div>
    </RequireAuth>
  )
}
