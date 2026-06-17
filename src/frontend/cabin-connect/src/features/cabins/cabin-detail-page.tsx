import { useEffect, useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { apiFetch } from '../../lib/api-client'
import { useAuth } from '../auth/use-auth'
import type { CabinDetail } from './cabin-types'

const AMENITY_LABELS: Record<string, string> = {
  'wifi':         'Wi-Fi',
  'parking':      'Parking',
  'hot-tub':      'Hot Tub',
  'pet-friendly': 'Pet Friendly',
  'fireplace':    'Fireplace',
  'kitchen':      'Kitchen',
}

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
  const { id }      = useParams<{ id: string }>()
  const location    = useLocation()
  const navigate    = useNavigate()
  const { user, loading: authLoading } = useAuth()

  const preState    = location.state as { checkIn?: string; checkOut?: string } | null
  const today       = new Date().toISOString().slice(0, 10)
  const tomorrow    = new Date(Date.now() + 86_400_000).toISOString().slice(0, 10)

  const [cabin, setCabin]               = useState<CabinDetail | null>(null)
  const [cabinError, setCabinError]     = useState<string | null>(null)
  const [checkIn, setCheckIn]           = useState(preState?.checkIn ?? today)
  const [checkOut, setCheckOut]         = useState(preState?.checkOut ?? tomorrow)
  const [reserving, setReserving]       = useState(false)
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
  const datesValid     = nights >= 1

  async function handleReserve() {
    if (!datesValid || !cabin || !id) return

    if (!authLoading && !user) {
      const returnUrl = encodeURIComponent(location.pathname + location.search)
      navigate(`/login?returnUrl=${returnUrl}`)
      return
    }

    setReserving(true)
    setReserveError(null)

    try {
      const res  = await apiFetch('/api/holds', {
        method: 'POST',
        body: JSON.stringify({ cabinId: id, checkIn, checkOut }),
      })
      const json = await res.json()

      if (res.status === 409 && json.error?.code === 'CABIN_UNAVAILABLE') {
        setReserveError('These dates are no longer available. Please select different dates.')
        return
      }
      if (!res.ok) {
        setReserveError(json.error?.message ?? 'Something went wrong. Please try again.')
        return
      }

      const hold = json.data as { holdId: string; expiresAt: string }

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
      navigate(`/checkout?holdId=${hold.holdId}`, { state: holdState })
    } catch {
      setReserveError('Network error. Please try again.')
    } finally {
      setReserving(false)
    }
  }

  if (cabinError) {
    return (
      <div className="page" style={{ paddingTop: '2rem' }}>
        <p role="alert" style={{ color: 'var(--error)', marginBottom: '1rem' }}>{cabinError}</p>
        <Link to="/" className="back-link">← Back to search</Link>
      </div>
    )
  }

  if (!cabin) {
    return (
      <div className="page" style={{ paddingTop: '2rem' }}>
        <p role="status" className="text-muted">Loading cabin details…</p>
      </div>
    )
  }

  const fmt = new Intl.NumberFormat('en-US', { style: 'currency', currency: cabin.currency })

  return (
    <div className="page">
      <Link to="/" className="back-link" style={{ marginTop: '1.5rem', display: 'inline-flex' }}>
        ← Back to search
      </Link>

      {cabin.imageUrl
        ? <img src={cabin.imageUrl} alt={cabin.name} className="cabin-image-hero" />
        : <div className="cabin-image-placeholder" aria-hidden="true">🏕</div>
      }

      <h1 style={{ marginBottom: '0.5rem' }}>{cabin.name}</h1>

      <p style={{ color: 'var(--text-muted)', marginBottom: '0.375rem' }}>
        Up to {cabin.maxGuests} guests
      </p>

      {cabin.description && (
        <p style={{ marginBottom: '1rem', lineHeight: '1.65' }}>{cabin.description}</p>
      )}

      {cabin.amenities.length > 0 && (
        <ul className="amenity-list" aria-label="Amenities">
          {cabin.amenities.map(a => (
            <li key={a} className="tag" style={{ fontSize: '0.875rem', padding: '0.3rem 0.75rem' }}>
              {AMENITY_LABELS[a] ?? a}
            </li>
          ))}
        </ul>
      )}

      <div className="detail-section">
        <h2>Select your dates</h2>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem', marginTop: '1rem' }}>
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
        </div>

        {datesValid && (
          <div className="summary-card" style={{ marginTop: '1.25rem', marginBottom: 0 }}>
            <div className="summary-row">
              <span className="summary-label">
                {nights} {nights === 1 ? 'night' : 'nights'} × {fmt.format(cabin.baseRate)}
              </span>
              <span className="summary-value">{fmt.format(estimatedTotal)}</span>
            </div>
            <div className="summary-row summary-row--total">
              <span className="summary-label">Estimated total</span>
              <span className="summary-value">{fmt.format(estimatedTotal)}</span>
            </div>
          </div>
        )}

        {reserveError && (
          <p role="alert" className="form-error" style={{ marginTop: '1rem', marginBottom: 0 }}>
            {reserveError}
          </p>
        )}

        <button
          className="btn btn-primary btn-primary-lg"
          onClick={handleReserve}
          disabled={!datesValid || reserving}
          title={!datesValid ? 'Select valid check-in and check-out dates (minimum 1 night)' : undefined}
          style={{ marginTop: '1.25rem', width: '100%' }}
        >
          {reserving ? 'Placing hold…' : 'Reserve'}
        </button>
      </div>
    </div>
  )
}
