import { useState, useEffect } from 'react'
import { type SearchParams, AMENITY_SLUGS } from './search-types'

const AMENITY_LABELS: Record<string, string> = {
  'wifi':         'Wi-Fi',
  'parking':      'Parking',
  'hot-tub':      'Hot Tub',
  'pet-friendly': 'Pet Friendly',
  'fireplace':    'Fireplace',
  'kitchen':      'Kitchen',
}

interface Props {
  onSearch: (params: SearchParams) => void
  initialValues?: Partial<SearchParams>
}

function validateDates(checkIn: string, checkOut: string): string | null {
  if (!checkIn || !checkOut) return null
  if (checkIn === checkOut) return 'Minimum stay is 1 night'
  if (checkOut < checkIn) return 'Check-out must be after check-in'
  return null
}

export function SearchForm({ onSearch, initialValues }: Props) {
  const [checkIn, setCheckIn]   = useState(initialValues?.checkIn ?? '')
  const [checkOut, setCheckOut] = useState(initialValues?.checkOut ?? '')
  const [guests, setGuests]     = useState<number | ''>(initialValues?.guests ?? '')
  const [amenities, setAmenities] = useState<string[]>(initialValues?.amenities ?? [])
  const [minPrice, setMinPrice] = useState<number | ''>(initialValues?.minPrice ?? '')
  const [maxPrice, setMaxPrice] = useState<number | ''>(initialValues?.maxPrice ?? '')
  const [dateError, setDateError] = useState<string | null>(null)

  const today = new Date().toISOString().split('T')[0]

  useEffect(() => {
    setDateError(validateDates(checkIn, checkOut))
  }, [checkIn, checkOut])

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    const error = validateDates(checkIn, checkOut)
    if (error) { setDateError(error); return }

    const params: SearchParams = { checkIn, checkOut }
    if (guests !== '')        params.guests   = guests
    if (amenities.length > 0) params.amenities = amenities
    if (minPrice !== '')      params.minPrice = minPrice
    if (maxPrice !== '')      params.maxPrice = maxPrice

    onSearch(params)
  }

  function toggleAmenity(slug: string) {
    setAmenities(prev =>
      prev.includes(slug) ? prev.filter(a => a !== slug) : [...prev, slug],
    )
  }

  return (
    <form onSubmit={handleSubmit} noValidate>
      <div className="search-form__primary">
        <div>
          <label htmlFor="check-in">Check-in</label>
          <input
            id="check-in"
            type="date"
            min={today}
            value={checkIn}
            onChange={e => setCheckIn(e.target.value)}
            required
          />
        </div>

        <div>
          <label htmlFor="check-out">Check-out</label>
          <input
            id="check-out"
            type="date"
            min={checkIn || today}
            value={checkOut}
            onChange={e => setCheckOut(e.target.value)}
            required
          />
        </div>

        <div>
          <label htmlFor="guests">Guests</label>
          <input
            id="guests"
            type="number"
            min={1}
            placeholder="Any"
            value={guests}
            onChange={e => setGuests(e.target.value === '' ? '' : Number(e.target.value))}
          />
        </div>

        <button type="submit" className="btn btn-primary" style={{ alignSelf: 'flex-end', height: '42px' }}>
          Search
        </button>
      </div>

      {dateError && <p className="search-form__error" role="alert">{dateError}</p>}

      <div className="search-form__secondary">
        <div className="search-form__amenities-wrap">
          <span style={{ fontSize: '0.75rem', fontWeight: 700, letterSpacing: '0.06em', textTransform: 'uppercase', color: 'var(--text-h)' }}>Amenities</span>
          <div className="amenity-pills">
            {AMENITY_SLUGS.map(slug => (
              <label key={slug} className="amenity-pill">
                <input
                  type="checkbox"
                  value={slug}
                  checked={amenities.includes(slug)}
                  onChange={() => toggleAmenity(slug)}
                />
                {AMENITY_LABELS[slug] ?? slug}
              </label>
            ))}
          </div>
        </div>

        <div>
          <label htmlFor="min-price">Min price / night</label>
          <input
            id="min-price"
            type="number"
            min={0}
            placeholder="No min"
            value={minPrice}
            onChange={e => setMinPrice(e.target.value === '' ? '' : Number(e.target.value))}
          />
        </div>

        <div>
          <label htmlFor="max-price">Max price / night</label>
          <input
            id="max-price"
            type="number"
            min={0}
            placeholder="No max"
            value={maxPrice}
            onChange={e => setMaxPrice(e.target.value === '' ? '' : Number(e.target.value))}
          />
        </div>
      </div>
    </form>
  )
}
