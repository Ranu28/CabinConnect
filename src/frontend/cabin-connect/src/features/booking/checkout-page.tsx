import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useNavigate, useSearchParams, useLocation } from 'react-router-dom'
import { apiFetch } from '../../lib/api-client'
import { RequireAuth } from '../auth/require-auth'
import type { BookingConfirmState, HoldCheckoutState } from './booking-types'

function formatMMSS(totalSeconds: number): string {
  const m = Math.max(0, Math.floor(totalSeconds / 60))
  const s = Math.max(0, totalSeconds % 60)
  return `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`
}

function CheckoutContent() {
  const [searchParams]  = useSearchParams()
  const location        = useLocation()
  const navigate        = useNavigate()
  const holdId          = searchParams.get('holdId') ?? ''

  // Load hold state from router state (fresh navigation) or sessionStorage (page refresh).
  const stored = holdId ? sessionStorage.getItem(`hold:${holdId}`) : null
  const initial = (location.state as HoldCheckoutState | null)
    ?? (stored ? (JSON.parse(stored) as HoldCheckoutState) : null)

  const [holdState]        = useState<HoldCheckoutState | null>(initial)
  const [secondsLeft, setSecondsLeft] = useState<number>(() => {
    if (!initial?.expiresAt) return 0
    return Math.max(0, Math.floor((new Date(initial.expiresAt).getTime() - Date.now()) / 1000))
  })
  const [confirming, setConfirming] = useState(false)
  const [confirmError, setConfirmError] = useState<string | null>(null)

  const expired = secondsLeft <= 0

  // Redirect if holdId is missing or hold data is unavailable (AC-7).
  useEffect(() => {
    if (!holdId || !holdState) {
      navigate('/', { replace: true })
    }
  }, [holdId, holdState, navigate])

  // AC-2: count down to expiresAt in real time; does not reset on refresh because
  // secondsLeft is initialised from the server's expiresAt, not from a fixed duration.
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null)
  useEffect(() => {
    if (!holdState?.expiresAt || expired) return
    intervalRef.current = setInterval(() => {
      setSecondsLeft(Math.max(0, Math.floor((new Date(holdState.expiresAt).getTime() - Date.now()) / 1000)))
    }, 1000)
    return () => { if (intervalRef.current) clearInterval(intervalRef.current) }
  }, [holdState, expired])

  const handleConfirm = useCallback(async () => {
    if (!holdState || expired || !holdId) return
    setConfirming(true)
    setConfirmError(null)

    try {
      const res  = await apiFetch('/api/bookings', {
        method: 'POST',
        body: JSON.stringify({ holdId }),
      })
      const json = await res.json()

      // AC-5: API returned HOLD_EXPIRED — treat same as countdown reaching zero
      if (res.status === 409 && json.error?.code === 'HOLD_EXPIRED') {
        setSecondsLeft(0)
        return
      }
      if (!res.ok) {
        setConfirmError(json.error?.message ?? 'Something went wrong. Please try again.')
        return
      }

      const booking = json.data as { bookingId: string }

      // Pass booking details to the confirmation page via router state.
      const nights = Math.round(
        (new Date(holdState.checkOut).getTime() - new Date(holdState.checkIn).getTime()) / 86_400_000
      )
      const confirmState: BookingConfirmState = {
        bookingId:  booking.bookingId,
        cabinName:  holdState.cabinName,
        checkIn:    holdState.checkIn,
        checkOut:   holdState.checkOut,
        nights,
        totalPrice: holdState.totalPrice,
        currency:   holdState.currency,
      }

      sessionStorage.removeItem(`hold:${holdId}`)
      // AC-3: navigate to booking confirmed on success
      navigate(`/booking-confirmed?bookingId=${booking.bookingId}`, { state: confirmState })
    } catch {
      setConfirmError('Network error. Please try again.')
    } finally {
      setConfirming(false)
    }
  }, [holdState, expired, holdId, navigate])

  if (!holdState) return null

  const fmt = new Intl.NumberFormat('en-US', { style: 'currency', currency: holdState.currency })

  return (
    <main style={{ maxWidth: 600, margin: '2rem auto', padding: '0 1rem' }}>
      <h1>Complete your booking</h1>

      {/* AC-1: hold details */}
      <section aria-label="Booking summary">
        <h2>{holdState.cabinName}</h2>
        <p>Check-in: <strong>{holdState.checkIn}</strong></p>
        <p>Check-out: <strong>{holdState.checkOut}</strong></p>
        <p>Nights: <strong>{holdState.nights}</strong></p>
        <p>Total: <strong>{fmt.format(holdState.totalPrice)}</strong></p>
      </section>

      {/* AC-2: countdown */}
      <p aria-live="polite">
        {expired
          ? 'Your hold has expired.'
          : `Hold expires in: ${formatMMSS(secondsLeft)}`}
      </p>

      {/* AC-4: expired state */}
      {expired && (
        <p>
          Your hold has expired. <Link to="/">Search again</Link>
        </p>
      )}

      {confirmError && <p role="alert" style={{ color: 'red' }}>{confirmError}</p>}

      {/* AC-4: button disabled when expired */}
      <button onClick={handleConfirm} disabled={expired || confirming}>
        {confirming ? 'Confirming…' : 'Confirm Booking'}
      </button>
    </main>
  )
}

export function CheckoutPage() {
  return (
    <RequireAuth>
      <CheckoutContent />
    </RequireAuth>
  )
}
