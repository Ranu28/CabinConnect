import type { CabinSearchResult } from './search-types'

const MAX_VISIBLE_AMENITIES = 4

interface Props {
  result: CabinSearchResult
  onSelect: (cabinId: string) => void
}

export function CabinResultCard({ result, onSelect }: Props) {
  const { cabinId, name, imageUrl, maxGuests, amenities, priceBreakdown, totalPrice, currency } =
    result

  const visibleAmenities = amenities.slice(0, MAX_VISIBLE_AMENITIES)
  const overflowCount = amenities.length - MAX_VISIBLE_AMENITIES

  const formatCurrency = (amount: number) =>
    new Intl.NumberFormat('en-US', { style: 'currency', currency }).format(amount)

  const formattedTotal = formatCurrency(totalPrice)

  function handleKeyDown(e: React.KeyboardEvent) {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault()
      onSelect(cabinId)
    }
  }

  return (
    <article
      aria-label={`View ${name} — ${formattedTotal} total`}
      onClick={() => onSelect(cabinId)}
      onKeyDown={handleKeyDown}
      tabIndex={0}
      style={{ cursor: 'pointer' }}
    >
      {imageUrl ? (
        <img src={imageUrl} alt={name} />
      ) : (
        <div role="img" aria-label={`No image available for ${name}`}>
          <span aria-hidden="true">🏕</span>
        </div>
      )}

      <h2>{name}</h2>
      <p>Up to {maxGuests} guests</p>

      {amenities.length > 0 && (
        <ul aria-label="Amenities">
          {visibleAmenities.map(a => (
            <li key={a}>{a}</li>
          ))}
          {overflowCount > 0 && <li>+{overflowCount} more</li>}
        </ul>
      )}

      <ul aria-label="Price breakdown">
        {priceBreakdown.map((item, i) => (
          <li key={i}>
            {item.label} = {formatCurrency(item.subtotal)}
          </li>
        ))}
      </ul>

      <p>
        <strong aria-label={`Total price ${formattedTotal}`}>{formattedTotal}</strong>
      </p>
    </article>
  )
}
