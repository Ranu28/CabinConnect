import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { SearchResultsView } from './search-results-view'
import type { CabinSearchResult } from './search-types'

// ── Fixtures ──────────────────────────────────────────────────────────────────

const CABIN_A: CabinSearchResult = {
  cabinId: 'cabin-a',
  name: 'Cabin A',
  description: '',
  imageUrl: null,
  maxGuests: 4,
  amenities: [],
  location: null,
  priceBreakdown: [{ label: '2 nights × 100.00 (Base Rate)', nights: 2, ratePerNight: 100, rateType: 'BaseRate', subtotal: 200 }],
  totalPrice: 200,
  currency: 'USD',
}

const CABIN_B: CabinSearchResult = { ...CABIN_A, cabinId: 'cabin-b', name: 'Cabin B', totalPrice: 400 }

function makeApiResponse(
  items: CabinSearchResult[],
  totalCount = items.length,
  pageSize = 20,
  page = 1,
) {
  return { ok: true, json: async () => ({ data: { items, totalCount, pageSize, page } }) } as Response
}

function makeFetchMock(response: Response) {
  return vi.spyOn(globalThis, 'fetch').mockResolvedValue(response)
}

// ── Render helper ─────────────────────────────────────────────────────────────

function renderView(initialUrl = '/') {
  return render(
    <MemoryRouter initialEntries={[initialUrl]}>
      <Routes>
        <Route path="/" element={<SearchResultsView />} />
      </Routes>
    </MemoryRouter>,
  )
}

async function submitSearch(checkIn = '2026-07-01', checkOut = '2026-07-05') {
  const user = userEvent.setup()
  await user.type(screen.getByLabelText('Check-in'), checkIn)
  await user.type(screen.getByLabelText('Check-out'), checkOut)
  await user.click(screen.getByRole('button', { name: 'Search' }))
}

// ── Tests ─────────────────────────────────────────────────────────────────────

afterEach(() => vi.restoreAllMocks())

// AC-6: idle — only form visible, no results or empty state before first search
test('AC-6: shows only search form before any search is submitted', () => {
  renderView()

  expect(screen.getByLabelText('Check-in')).toBeInTheDocument()
  expect(screen.queryByRole('list', { name: 'Search results' })).not.toBeInTheDocument()
  expect(screen.queryByRole('status')).not.toBeInTheDocument()
  expect(screen.queryByText(/no cabins available/i)).not.toBeInTheDocument()
})

// AC-1: results displayed after successful search
test('AC-1: renders a result card for each cabin returned by the API', async () => {
  makeFetchMock(makeApiResponse([CABIN_A, CABIN_B]))
  renderView()

  await submitSearch()

  await waitFor(() => {
    expect(screen.getByText('Cabin A')).toBeInTheDocument()
    expect(screen.getByText('Cabin B')).toBeInTheDocument()
  })
})

// AC-2: loading state shown while request is in flight
test('AC-2: shows loading indicator while the API request is in flight', async () => {
  let resolveResponse!: (r: Response) => void
  vi.spyOn(globalThis, 'fetch').mockReturnValue(
    new Promise<Response>(r => { resolveResponse = r }),
  )
  renderView()

  await submitSearch()

  expect(screen.getByRole('status', { name: 'Loading results' })).toBeInTheDocument()

  resolveResponse(makeApiResponse([]))
  await waitFor(() => expect(screen.queryByRole('status')).not.toBeInTheDocument())
})

// AC-3: empty state message when API returns no results
test('AC-3: shows empty-state message when API returns zero results', async () => {
  makeFetchMock(makeApiResponse([]))
  renderView()

  await submitSearch()

  await waitFor(() =>
    expect(
      screen.getByText(/no cabins available for these dates/i),
    ).toBeInTheDocument(),
  )
  expect(screen.queryByRole('list', { name: 'Search results' })).not.toBeInTheDocument()
})

// AC-4: error state — no partial results shown
test('AC-4: shows error alert and no results when the API request fails', async () => {
  vi.spyOn(globalThis, 'fetch').mockResolvedValue({ ok: false, json: async () => ({}) } as Response)
  renderView()

  await submitSearch()

  await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
  expect(screen.queryByRole('list', { name: 'Search results' })).not.toBeInTheDocument()
})

// AC-5: re-submit with new params updates results
test('AC-5: re-search with new parameters updates the results list', async () => {
  const fetchSpy = vi.spyOn(globalThis, 'fetch')
    .mockResolvedValueOnce(makeApiResponse([CABIN_A]))
    .mockResolvedValueOnce(makeApiResponse([CABIN_B]))
  renderView()

  await submitSearch('2026-07-01', '2026-07-03')
  await waitFor(() => expect(screen.getByText('Cabin A')).toBeInTheDocument())

  await submitSearch('2026-08-01', '2026-08-05')
  await waitFor(() => expect(screen.getByText('Cabin B')).toBeInTheDocument())

  expect(screen.queryByText('Cabin A')).not.toBeInTheDocument()
  expect(fetchSpy).toHaveBeenCalledTimes(2)
})

// AC-7: pagination controls shown when totalCount > pageSize
test('AC-7: shows pagination controls when there are multiple pages', async () => {
  makeFetchMock(makeApiResponse([CABIN_A], 25, 20))
  renderView()

  await submitSearch()

  await waitFor(() =>
    expect(screen.getByRole('navigation', { name: 'Pagination' })).toBeInTheDocument(),
  )
  expect(screen.getByText('Page 1 of 2')).toBeInTheDocument()
})

// AC-7 inverse: no pagination when all results fit on one page
test('AC-7: hides pagination when all results fit on one page', async () => {
  makeFetchMock(makeApiResponse([CABIN_A, CABIN_B], 2, 20))
  renderView()

  await submitSearch()

  await waitFor(() => expect(screen.getByText('Cabin A')).toBeInTheDocument())
  expect(screen.queryByRole('navigation', { name: 'Pagination' })).not.toBeInTheDocument()
})

// AC-8: clicking Next calls API with page=2
test('AC-8: clicking Next page calls the API with page=2', async () => {
  const user = userEvent.setup()
  const fetchSpy = vi.spyOn(globalThis, 'fetch')
    .mockResolvedValue(makeApiResponse([CABIN_A], 25, 20))
  renderView()

  await submitSearch()
  await waitFor(() => screen.getByRole('navigation', { name: 'Pagination' }))

  await user.click(screen.getByRole('button', { name: 'Next' }))

  await waitFor(() =>
    expect(fetchSpy).toHaveBeenCalledTimes(2),
  )
  const secondCallUrl = fetchSpy.mock.calls[1][0] as string
  expect(secondCallUrl).toContain('page=2')
})

// AC-9: Next button is disabled on the last page
test('AC-9: Next page button is disabled when on the last page', async () => {
  const user = userEvent.setup()
  // 2 cabins, 1 per page → 2 pages; same response for both fetches
  vi.spyOn(globalThis, 'fetch').mockResolvedValue(makeApiResponse([CABIN_A], 2, 1))
  renderView()

  await submitSearch()
  await waitFor(() => screen.getByRole('navigation', { name: 'Pagination' }))

  // Navigate to page 2 — the last page
  await user.click(screen.getByRole('button', { name: 'Next' }))
  await waitFor(() => expect(screen.getByRole('button', { name: 'Next' })).toBeDisabled())
})

// AC-10: new search resets to page 1
test('AC-10: submitting a new search while on page 2 resets pagination to page 1', async () => {
  const user = userEvent.setup()
  const fetchSpy = vi.spyOn(globalThis, 'fetch')
    .mockResolvedValue(makeApiResponse([CABIN_A], 25, 20))
  renderView()

  // First search
  await submitSearch()
  await waitFor(() => screen.getByRole('navigation', { name: 'Pagination' }))

  // Go to page 2
  await user.click(screen.getByRole('button', { name: 'Next' }))
  await waitFor(() => expect(fetchSpy).toHaveBeenCalledTimes(2))

  // Submit new search
  await submitSearch('2026-08-01', '2026-08-10')

  await waitFor(() => expect(fetchSpy).toHaveBeenCalledTimes(3))
  const thirdCallUrl = fetchSpy.mock.calls[2][0] as string
  expect(thirdCallUrl).toContain('page=1')
})
