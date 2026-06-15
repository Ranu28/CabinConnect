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

  // Use router state if available (freshly navigated from Checkout), otherwise fetch from API.
  const stateData = location.state as BookingConfirmState | null
  const [detail, setDetail]   = useState<BookingConfirmState | null>(stateData)
  const [loading, setLoading] = useState(!stateData)

  useEffect(() => {
    if (stateData || !bookingId) return
    setLoading(true)
    apiFetch(`/api/bookings/${bookingId}`)
      .then(r => r.json())
      .then(json => {
        if (json.error || !json.data) {
          // AC-5: invalid bookingId — redirect to /
          navigate('/', { replace: true })
          return
        }
        const b = json.data as GuestBookingItem
        const nights = Math.round(
          (new Date(b.checkOut).getTime() - new Date(b.checkIn).getTime()) / 86_400_000
        )
        setDetail({ bookingId: b.bookingId, cabinName: b.cabinName, checkIn: b.checkIn, checkOut: b.checkOut, nights, totalPrice: b.totalPrice, currency: b.currency })
      })
      .catch(() => navigate('/', { replace: true }))
      .finally(() => setLoading(false))
  }, [bookingId, stateData, navigate])

  if (!bookingId) {
    navigate('/', { replace: true })
    return null
  }

  if (loading) return <p>Loading booking details…</p>
  if (!detail)  return null

  const fmt = new Intl.NumberFormat('en-US', { style: 'currency', currency: detail.currency })

  return (
    <main style={{ maxWidth: 600, margin: '2rem auto', padding: '0 1rem' }}>
      {/* AC-2: success heading */}
      <h1>Your booking is confirmed!</h1>

      {/* AC-1: booking details */}
      <section aria-label="Booking details">
        <p>Booking reference: <strong>{detail.bookingId}</strong></p>
        <h2>{detail.cabinName}</h2>
        <p>Check-in: <strong>{detail.checkIn}</strong></p>
        <p>Check-out: <strong>{detail.checkOut}</strong></p>
        <p>Nights: <strong>{detail.nights}</strong></p>
        <p>Total paid: <strong>{fmt.format(detail.totalPrice)}</strong></p>
      </section>

      {/* AC-3: link to My Bookings */}
      <p><Link to="/my-bookings">View my bookings</Link></p>
      {/* AC-4: link back to search */}
      <p><Link to="/">Back to search</Link></p>
    </main>
  )
}

export function BookingConfirmedPage() {
  return (
    <RequireAuth>
      <BookingConfirmedContent />
    </RequireAuth>
  )
}
