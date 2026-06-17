import { useEffect, useState } from 'react'
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router-dom'
import { apiFetch } from '../../lib/api-client'
import { RequireAuth } from '../auth/require-auth'
import type { BookingConfirmState, GuestBookingItem } from './booking-types'

function BookingConfirmedContent() {
  const [searchParams] = useSearchParams()
  const location       = useLocation()
  const navigate       = useNavigate()
  const bookingId      = searchParams.get('bookingId') ?? ''

  const stateData = location.state as BookingConfirmState | null
  const [detail, setDetail]   = useState<BookingConfirmState | null>(stateData)
  const [loading, setLoading] = useState(!stateData)

  useEffect(() => {
    if (stateData || !bookingId) return
    setLoading(true)
    apiFetch(`/api/bookings/${bookingId}`)
      .then(r => r.json())
      .then(json => {
        if (json.error || !json.data) { navigate('/', { replace: true }); return }
        const b = json.data as GuestBookingItem
        const nights = Math.round(
          (new Date(b.checkOut).getTime() - new Date(b.checkIn).getTime()) / 86_400_000
        )
        setDetail({ bookingId: b.bookingId, cabinName: b.cabinName, checkIn: b.checkIn, checkOut: b.checkOut, nights, totalPrice: b.totalPrice, currency: b.currency })
      })
      .catch(() => navigate('/', { replace: true }))
      .finally(() => setLoading(false))
  }, [bookingId, stateData, navigate])

  if (!bookingId) { navigate('/', { replace: true }); return null }
  if (loading) return <p className="text-muted" style={{ padding: '3rem 1.5rem', textAlign: 'center' }}>Loading booking details…</p>
  if (!detail)  return null

  const fmt = new Intl.NumberFormat('en-US', { style: 'currency', currency: detail.currency })

  return (
    <div className="page" style={{ maxWidth: 560, textAlign: 'center' }}>
      <div style={{ marginTop: '2rem' }}>
        <div className="confirmed-icon" aria-hidden="true">✓</div>

        <h1 style={{ marginBottom: '0.5rem' }}>Booking confirmed!</h1>
        <p className="text-muted" style={{ marginBottom: '2rem' }}>
          You're all set. We've reserved your cabin.
        </p>

        <div className="summary-card" style={{ textAlign: 'left' }} aria-label="Booking details">
          <p style={{ fontSize: '0.8125rem', color: 'var(--text-muted)', marginBottom: '0.75rem', fontFamily: 'var(--mono)' }}>
            Ref: {detail.bookingId}
          </p>
          <h2 style={{ marginBottom: '1rem' }}>{detail.cabinName}</h2>

          <div className="summary-row">
            <span className="summary-label">Check-in</span>
            <span className="summary-value">{detail.checkIn}</span>
          </div>
          <div className="summary-row">
            <span className="summary-label">Check-out</span>
            <span className="summary-value">{detail.checkOut}</span>
          </div>
          <div className="summary-row">
            <span className="summary-label">Nights</span>
            <span className="summary-value">{detail.nights}</span>
          </div>
          <div className="summary-row summary-row--total">
            <span className="summary-label">Total paid</span>
            <span className="summary-value">{fmt.format(detail.totalPrice)}</span>
          </div>
        </div>

        <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'center', flexWrap: 'wrap', marginTop: '0.5rem' }}>
          <Link to="/my-bookings" className="btn btn-primary">View my bookings</Link>
          <Link to="/" className="btn btn-ghost">Search more cabins</Link>
        </div>
      </div>
    </div>
  )
}

export function BookingConfirmedPage() {
  return (
    <RequireAuth>
      <BookingConfirmedContent />
    </RequireAuth>
  )
}
