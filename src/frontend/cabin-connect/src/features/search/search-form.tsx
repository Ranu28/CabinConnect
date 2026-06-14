import { useState, useEffect } from 'react'
import { type SearchParams, AMENITY_SLUGS } from './search-types'

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
  const [checkIn, setCheckIn] = useState(initialValues?.checkIn ?? '')
  const [checkOut, setCheckOut] = useState(initialValues?.checkOut ?? '')
  const [guests, setGuests] = useState<number | ''>(initialValues?.guests ?? '')
  const [amenities, setAmenities] = useState<string[]>(initialValues?.amenities ?? [])
  const [minPrice, setMinPrice] = useState<number | ''>(initialValues?.minPrice ?? '')
  const [maxPrice, setMaxPrice] = useState<number | ''>(initialValues?.maxPrice ?? '')
  const [dateError, setDateError] = useState<string | null>(null)

  const today = new Date().toISOString().split('T')[0]

  // AC-6: re-validate immediately when either date changes
  useEffect(() => {
    setDateError(validateDates(checkIn, checkOut))
  }, [checkIn, checkOut])

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    const error = validateDates(checkIn, checkOut)
    if (error) {
      setDateError(error)
      return
    }

    // AC-4: dates are already ISO 8601 date-only strings from <input type="date">
    const params: SearchParams = { checkIn, checkOut }
    // AC-5: omit optional fields when not provided
    if (guests !== '') params.guests = guests
    if (amenities.length > 0) params.amenities = amenities
    if (minPrice !== '') params.minPrice = minPrice
    if (maxPrice !== '') params.maxPrice = maxPrice

    onSearch(params)
  }

  function toggleAmenity(slug: string) {
    setAmenities(prev =>
      prev.includes(slug) ? prev.filter(a => a !== slug) : [...prev, slug],
    )
  }

  return (
    <form onSubmit={handleSubmit} noValidate>
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

      {dateError && <p role="alert">{dateError}</p>}

      <div>
        <label htmlFor="guests">Guests</label>
        <input
          id="guests"
          type="number"
          min={1}
          value={guests}
          onChange={e => setGuests(e.target.value === '' ? '' : Number(e.target.value))}
        />
      </div>

      <fieldset>
        <legend>Amenities</legend>
        {AMENITY_SLUGS.map(slug => (
          <label key={slug}>
            <input
              type="checkbox"
              value={slug}
              checked={amenities.includes(slug)}
              onChange={() => toggleAmenity(slug)}
            />
            {' '}{slug}
          </label>
        ))}
      </fieldset>

      <div>
        <label htmlFor="min-price">Min price / night</label>
        <input
          id="min-price"
          type="number"
          min={0}
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
          value={maxPrice}
          onChange={e => setMaxPrice(e.target.value === '' ? '' : Number(e.target.value))}
        />
      </div>

      <button type="submit">Search</button>
    </form>
  )
}
