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
  const [searchParams] = useSearchParams()
  const location       = useLocation()
  const navigate       = useNavigate()
  const holdId         = searchParams.get('holdId') ?? ''

  const stored  = holdId ? sessionStorage.getItem(`hold:${holdId}`) : null
  const initial = (location.state as HoldCheckoutState | null)
    ?? (stored ? (JSON.parse(stored) as HoldCheckoutState) : null)

  const [holdState]    = useState<HoldCheckoutState | null>(initial)
  const [secondsLeft, setSecondsLeft] = useState<number>(() => {
    if (!initial?.expiresAt) return 0
    return Math.max(0, Math.floor((new Date(initial.expiresAt).getTime() - Date.now()) / 1000))
  })
  const [confirming, setConfirming]     = useState(false)
  const [confirmError, setConfirmError] = useState<string | null>(null)

  const expired = secondsLeft <= 0

  useEffect(() => {
    if (!holdId || !holdState) navigate('/', { replace: true })
  }, [holdId, holdState, navigate])

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

      if (res.status === 409 && json.error?.code === 'HOLD_EXPIRED') {
        setSecondsLeft(0)
        return
      }
      if (!res.ok) {
        setConfirmError(json.error?.message ?? 'Something went wrong. Please try again.')
        return
      }

      const booking = json.data as { bookingId: string }

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
    <div className="page" style={{ maxWidth: 560 }}>
      <h1 style={{ marginTop: '1.5rem', marginBottom: '1.75rem' }}>Complete your booking</h1>

      <div className="summary-card" aria-label="Booking summary">
        <h2 style={{ marginBottom: '1rem' }}>{holdState.cabinName}</h2>
        <div className="summary-row">
          <span className="summary-label">Check-in</span>
          <span className="summary-value">{holdState.checkIn}</span>
        </div>
        <div className="summary-row">
          <span className="summary-label">Check-out</span>
          <span className="summary-value">{holdState.checkOut}</span>
        </div>
        <div className="summary-row">
          <span className="summary-label">Nights</span>
          <span className="summary-value">{holdState.nights}</span>
        </div>
        <div className="summary-row summary-row--total">
          <span className="summary-label">Total</span>
          <span className="summary-value">{fmt.format(holdState.totalPrice)}</span>
        </div>
      </div>

      <div aria-live="polite" className={`countdown${expired ? ' countdown--urgent' : ''}`}>
        {expired
          ? '⏱ Hold expired'
          : `⏱ Hold expires in ${formatMMSS(secondsLeft)}`
        }
      </div>

      {expired && (
        <p style={{ color: 'var(--text-muted)', marginBottom: '1rem' }}>
          Your hold has expired.{' '}
          <Link to="/">Search again</Link>
        </p>
      )}

      {confirmError && (
        <p role="alert" className="form-error">{confirmError}</p>
      )}

      <button
        className="btn btn-primary btn-primary-lg"
        onClick={handleConfirm}
        disabled={expired || confirming}
        style={{ width: '100%', marginTop: '0.5rem' }}
      >
        {confirming ? 'Confirming…' : 'Confirm Booking'}
      </button>
    </div>
  )
}

export function CheckoutPage() {
  return (
    <RequireAuth>
      <CheckoutContent />
    </RequireAuth>
  )
}
