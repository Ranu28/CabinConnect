import type { CabinSearchResult } from './search-types'

const MAX_VISIBLE_AMENITIES = 4

const AMENITY_LABELS: Record<string, string> = {
  'wifi':         'Wi-Fi',
  'parking':      'Parking',
  'hot-tub':      'Hot Tub',
  'pet-friendly': 'Pet Friendly',
  'fireplace':    'Fireplace',
  'kitchen':      'Kitchen',
}

interface Props {
  result: CabinSearchResult
  onSelect: (cabinId: string) => void
}

export function CabinResultCard({ result, onSelect }: Props) {
  const { cabinId, name, imageUrl, maxGuests, amenities, totalPrice, currency } = result

  const visibleAmenities = amenities.slice(0, MAX_VISIBLE_AMENITIES)
  const overflowCount    = amenities.length - MAX_VISIBLE_AMENITIES

  const formattedTotal = new Intl.NumberFormat('en-US', { style: 'currency', currency }).format(totalPrice)

  function handleKeyDown(e: React.KeyboardEvent) {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault()
      onSelect(cabinId)
    }
  }

  return (
    <article
      className="cabin-card"
      aria-label={`View ${name} — ${formattedTotal} total`}
      onClick={() => onSelect(cabinId)}
      onKeyDown={handleKeyDown}
      tabIndex={0}
    >
      {imageUrl
        ? <img src={imageUrl} alt={name} className="cabin-card__image" />
        : (
          <div className="cabin-card__placeholder" role="img" aria-label={`No image for ${name}`}>
            🏕
          </div>
        )
      }

      <div className="cabin-card__body">
        <div className="cabin-card__name">{name}</div>
        <div className="cabin-card__meta">Up to {maxGuests} guests</div>

        {amenities.length > 0 && (
          <ul className="cabin-card__amenities" aria-label="Amenities">
            {visibleAmenities.map(a => (
              <li key={a} className="tag">{AMENITY_LABELS[a] ?? a}</li>
            ))}
            {overflowCount > 0 && (
              <li className="tag" style={{ background: 'var(--border)', color: 'var(--text-muted)' }}>
                +{overflowCount} more
              </li>
            )}
          </ul>
        )}

        <div className="cabin-card__footer">
          <span className="cabin-card__price" aria-label={`Total price ${formattedTotal}`}>
            {formattedTotal}
          </span>
          <span className="cabin-card__price-label">total</span>
        </div>
      </div>
    </article>
  )
}
