import { useEffect, useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { apiFetch } from '../../lib/api-client'
import { useAuth } from '../auth/use-auth'
import type { CabinDetail } from './cabin-types'

interface HoldState {
  cabinId:    string
  cabinName:  string
  checkIn:    string
  checkOut:   string
  nights:     number
  totalPrice: number
  currency:   string
  expiresAt:  string
}

export function CabinDetailPage() {
  const { id }            = useParams<{ id: string }>()
  const location          = useLocation()
  const navigate          = useNavigate()
  const { user, loading: authLoading } = useAuth()

  const preState          = location.state as { checkIn?: string; checkOut?: string } | null
  const today             = new Date().toISOString().slice(0, 10)
  const tomorrow          = new Date(Date.now() + 86_400_000).toISOString().slice(0, 10)

  const [cabin, setCabin]         = useState<CabinDetail | null>(null)
  const [cabinError, setCabinError] = useState<string | null>(null)
  const [checkIn, setCheckIn]     = useState(preState?.checkIn ?? today)
  const [checkOut, setCheckOut]   = useState(preState?.checkOut ?? tomorrow)
  const [reserving, setReserving] = useState(false)
  const [reserveError, setReserveError] = useState<string | null>(null)

  useEffect(() => {
    if (!id) return
    fetch(`/api/cabins/${id}`)
      .then(r => r.json())
      .then(json => {
        if (json.error) setCabinError('Cabin not found.')
        else setCabin(json.data as CabinDetail)
      })
      .catch(() => setCabinError('Failed to load cabin details.'))
  }, [id])

  const checkInDate  = checkIn  ? new Date(checkIn)  : null
  const checkOutDate = checkOut ? new Date(checkOut)  : null
  const nights       = checkInDate && checkOutDate
    ? Math.round((checkOutDate.getTime() - checkInDate.getTime()) / 86_400_000)
    : 0
  const estimatedTotal = cabin && nights > 0 ? nights * cabin.baseRate : 0
  const datesValid   = nights >= 1

  async function handleReserve() {
    if (!datesValid || !cabin || !id) return

    // AC-7: redirect unauthenticated Guests to login
    if (!authLoading && !user) {
      const returnUrl = encodeURIComponent(location.pathname + location.search)
      navigate(`/login?returnUrl=${returnUrl}`)
      return
    }

    setReserving(true)
    setReserveError(null)

    try {
      const res = await apiFetch('/api/holds', {
        method: 'POST',
        body: JSON.stringify({ cabinId: id, checkIn, checkOut }),
      })
      const json = await res.json()

      if (res.status === 409 && json.error?.code === 'CABIN_UNAVAILABLE') {
        // AC-6: unavailability error
        setReserveError('These dates are no longer available. Please select different dates.')
        return
      }
      if (!res.ok) {
        setReserveError(json.error?.message ?? 'Something went wrong. Please try again.')
        return
      }

      const hold = json.data as { holdId: string; expiresAt: string }

      // Store hold details in sessionStorage so the Checkout page survives a refresh.
      const holdState: HoldState = {
        cabinId:    id,
        cabinName:  cabin.name,
        checkIn,
        checkOut,
        nights,
        totalPrice: estimatedTotal,
        currency:   cabin.currency,
        expiresAt:  hold.expiresAt,
      }
      sessionStorage.setItem(`hold:${hold.holdId}`, JSON.stringify(holdState))

      // AC-5: navigate to Checkout
      navigate(`/checkout?holdId=${hold.holdId}`, { state: holdState })
    } catch {
      setReserveError('Network error. Please try again.')
    } finally {
      setReserving(false)
    }
  }

  if (cabinError) {
    return (
      <main style={{ maxWidth: 800, margin: '2rem auto', padding: '0 1rem' }}>
        <p role="alert">{cabinError}</p>
        <Link to="/">Back to search</Link>
      </main>
    )
  }

  if (!cabin) {
    return (
      <main style={{ maxWidth: 800, margin: '2rem auto', padding: '0 1rem' }}>
        <p role="status">Loading cabin details…</p>
      </main>
    )
  }

  return (
    <main style={{ maxWidth: 800, margin: '2rem auto', padding: '0 1rem' }}>
      <Link to="/">← Back to search</Link>

      {/* AC-1: cabin details */}
      {cabin.imageUrl && (
        <img src={cabin.imageUrl} alt={cabin.name} style={{ width: '100%', borderRadius: 8, marginTop: '1rem' }} />
      )}
      <h1>{cabin.name}</h1>
      <p>{cabin.description}</p>
      <p>Up to {cabin.maxGuests} guests</p>

      {cabin.amenities.length > 0 && (
        <ul aria-label="Amenities">
          {cabin.amenities.map(a => <li key={a}>{a}</li>)}
        </ul>
      )}

      {cabin.location && (
        <p>Location: {cabin.location.lat.toFixed(4)}, {cabin.location.lng.toFixed(4)}</p>
      )}

      {/* AC-2/AC-3/AC-4: date picker and price estimate */}
      <section aria-label="Select dates">
        <h2>Select your dates</h2>
        <div>
          <label htmlFor="check-in">Check-in</label>
          <input
            id="check-in"
            type="date"
            value={checkIn}
            min={today}
            onChange={e => setCheckIn(e.target.value)}
          />
        </div>
        <div>
          <label htmlFor="check-out">Check-out</label>
          <input
            id="check-out"
            type="date"
            value={checkOut}
            min={checkIn || today}
            onChange={e => setCheckOut(e.target.value)}
          />
        </div>

        {datesValid && (
          <p>
            {nights} {nights === 1 ? 'night' : 'nights'} ×{' '}
            {new Intl.NumberFormat('en-US', { style: 'currency', currency: cabin.currency }).format(cabin.baseRate)}
            {' '}= estimated{' '}
            <strong>
              {new Intl.NumberFormat('en-US', { style: 'currency', currency: cabin.currency }).format(estimatedTotal)}
            </strong>
          </p>
        )}

        {reserveError && <p role="alert" style={{ color: 'red' }}>{reserveError}</p>}

        {/* AC-4: button disabled when dates invalid */}
        <button
          onClick={handleReserve}
          disabled={!datesValid || reserving}
          title={!datesValid ? 'Select valid check-in and check-out dates (minimum 1 night)' : undefined}
        >
          {reserving ? 'Placing hold…' : 'Reserve'}
        </button>
      </section>
    </main>
  )
}
