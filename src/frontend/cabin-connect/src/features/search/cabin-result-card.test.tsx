import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { CabinResultCard } from './cabin-result-card'
import type { CabinSearchResult } from './search-types'

const BASE_RESULT: CabinSearchResult = {
  cabinId: 'cabin-1',
  name: 'Pine Ridge Cabin',
  description: 'A cozy cabin in the woods.',
  imageUrl: 'https://example.com/cabin.jpg',
  maxGuests: 6,
  amenities: ['wifi', 'parking', 'hot-tub', 'fireplace'],
  location: { lat: 60.0, lng: 10.0 },
  priceBreakdown: [
    {
      label: '4 nights × $120 (Base Rate)',
      nights: 4,
      ratePerNight: 120,
      rateType: 'BaseRate',
      subtotal: 480,
    },
  ],
  totalPrice: 480,
  currency: 'USD',
}

// AC-1: Base Rate only display
test('AC-1: renders name, image, guest count, amenity badges, rate line, and total price', () => {
  render(<CabinResultCard result={BASE_RESULT} onSelect={vi.fn()} />)

  expect(screen.getByRole('heading', { name: 'Pine Ridge Cabin' })).toBeInTheDocument()
  expect(screen.getByRole('img', { name: 'Pine Ridge Cabin' })).toBeInTheDocument()
  expect(screen.getByText('Up to 6 guests')).toBeInTheDocument()
  expect(screen.getByText('wifi')).toBeInTheDocument()
  expect(screen.getByText('4 nights × $120 (Base Rate) = $480.00')).toBeInTheDocument()
  expect(screen.getByText('$480.00')).toBeInTheDocument()
})

// AC-2: Multi-rate breakdown display
test('AC-2: renders each rate group as a separate line; total equals sum of subtotals', () => {
  const result: CabinSearchResult = {
    ...BASE_RESULT,
    priceBreakdown: [
      {
        label: '2 nights × $100 (Base Rate)',
        nights: 2,
        ratePerNight: 100,
        rateType: 'BaseRate',
        subtotal: 200,
      },
      {
        label: '3 nights × $150 (Summer Weekend)',
        nights: 3,
        ratePerNight: 150,
        rateType: 'SeasonalRate',
        subtotal: 450,
      },
    ],
    totalPrice: 650,
  }

  render(<CabinResultCard result={result} onSelect={vi.fn()} />)

  expect(screen.getByText('2 nights × $100 (Base Rate) = $200.00')).toBeInTheDocument()
  expect(screen.getByText('3 nights × $150 (Summer Weekend) = $450.00')).toBeInTheDocument()
  expect(screen.getByText('$650.00')).toBeInTheDocument()
})

// AC-3: Total Price is prominent (rendered inside <strong>)
test('AC-3: total price is displayed prominently with currency symbol', () => {
  render(<CabinResultCard result={BASE_RESULT} onSelect={vi.fn()} />)

  const total = screen.getByText('$480.00')
  expect(total.tagName).toBe('STRONG')
  expect(total).toHaveAccessibleName('Total price $480.00')
})

// AC-4: Missing imageUrl shows placeholder, no broken img element
test('AC-4: shows placeholder when imageUrl is null; no img element rendered', () => {
  render(<CabinResultCard result={{ ...BASE_RESULT, imageUrl: null }} onSelect={vi.fn()} />)

  expect(screen.queryByRole('img', { name: 'Pine Ridge Cabin' })).not.toBeInTheDocument()
  expect(screen.getByRole('img', { name: /no image available/i })).toBeInTheDocument()
})

// AC-5: Clicking the card calls onSelect with cabinId
test('AC-5: click calls onSelect with the correct cabinId', async () => {
  const user = userEvent.setup()
  const onSelect = vi.fn()
  render(<CabinResultCard result={BASE_RESULT} onSelect={onSelect} />)

  await user.click(screen.getByRole('article'))

  expect(onSelect).toHaveBeenCalledOnce()
  expect(onSelect).toHaveBeenCalledWith('cabin-1')
})

// AC-5 extension: keyboard Enter also triggers onSelect
test('AC-5: pressing Enter on card calls onSelect with the correct cabinId', async () => {
  const user = userEvent.setup()
  const onSelect = vi.fn()
  render(<CabinResultCard result={BASE_RESULT} onSelect={onSelect} />)

  screen.getByRole('article').focus()
  await user.keyboard('{Enter}')

  expect(onSelect).toHaveBeenCalledWith('cabin-1')
})

// Overflow: more than 4 amenities shows "+N more"
test('amenity overflow: shows max 4 badges plus "+N more" label', () => {
  const result: CabinSearchResult = {
    ...BASE_RESULT,
    amenities: ['wifi', 'parking', 'hot-tub', 'fireplace', 'kitchen', 'pet-friendly'],
  }

  render(<CabinResultCard result={result} onSelect={vi.fn()} />)

  expect(screen.getByText('wifi')).toBeInTheDocument()
  expect(screen.getByText('fireplace')).toBeInTheDocument()
  expect(screen.queryByText('kitchen')).not.toBeInTheDocument()
  expect(screen.getByText('+2 more')).toBeInTheDocument()
})
