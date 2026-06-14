import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { SearchForm } from './search-form'
import type { SearchParams } from './search-types'

function renderForm(onSearch = vi.fn()) {
  render(<SearchForm onSearch={onSearch} />)
  return {
    checkInInput: () => screen.getByLabelText('Check-in'),
    checkOutInput: () => screen.getByLabelText('Check-out'),
    submitButton: () => screen.getByRole('button', { name: 'Search' }),
    onSearch,
  }
}

// AC-1: Check-out before check-in is blocked
test('AC-1: blocks submission when check-out is before check-in', async () => {
  const user = userEvent.setup()
  const { checkInInput, checkOutInput, submitButton, onSearch } = renderForm()

  await user.type(checkInInput(), '2026-07-10')
  await user.type(checkOutInput(), '2026-07-05')
  await user.click(submitButton())

  expect(screen.getByRole('alert')).toBeInTheDocument()
  expect(onSearch).not.toHaveBeenCalled()
})

// AC-2: Same-day check-in/check-out is blocked with specific message
test('AC-2: shows "Minimum stay is 1 night" when check-in equals check-out', async () => {
  const user = userEvent.setup()
  const { checkInInput, checkOutInput, submitButton, onSearch } = renderForm()

  await user.type(checkInInput(), '2026-07-10')
  await user.type(checkOutInput(), '2026-07-10')
  await user.click(submitButton())

  expect(screen.getByRole('alert')).toHaveTextContent('Minimum stay is 1 night')
  expect(onSearch).not.toHaveBeenCalled()
})

// AC-3: Valid submission triggers onSearch with correct payload
test('AC-3: calls onSearch with correct payload on valid submission', async () => {
  const user = userEvent.setup()
  const onSearch = vi.fn()
  const { checkInInput, checkOutInput, submitButton } = renderForm(onSearch)

  await user.type(checkInInput(), '2026-07-01')
  await user.type(checkOutInput(), '2026-07-05')
  await user.click(submitButton())

  expect(onSearch).toHaveBeenCalledOnce()
  const params: SearchParams = onSearch.mock.calls[0][0]
  expect(params.checkIn).toBe('2026-07-01')
  expect(params.checkOut).toBe('2026-07-05')
})

// AC-4: Dates submitted as ISO 8601 date-only strings
test('AC-4: dates in payload are ISO 8601 date-only strings with no time component', async () => {
  const user = userEvent.setup()
  const onSearch = vi.fn()
  const { checkInInput, checkOutInput, submitButton } = renderForm(onSearch)

  await user.type(checkInInput(), '2026-08-15')
  await user.type(checkOutInput(), '2026-08-20')
  await user.click(submitButton())

  const params: SearchParams = onSearch.mock.calls[0][0]
  expect(params.checkIn).toMatch(/^\d{4}-\d{2}-\d{2}$/)
  expect(params.checkOut).toMatch(/^\d{4}-\d{2}-\d{2}$/)
})

// AC-5: Optional filters omitted when not filled in
test('AC-5: omits optional fields from payload when not filled in', async () => {
  const user = userEvent.setup()
  const onSearch = vi.fn()
  const { checkInInput, checkOutInput, submitButton } = renderForm(onSearch)

  await user.type(checkInInput(), '2026-07-01')
  await user.type(checkOutInput(), '2026-07-03')
  await user.click(submitButton())

  const params: SearchParams = onSearch.mock.calls[0][0]
  expect(params).not.toHaveProperty('guests')
  expect(params).not.toHaveProperty('amenities')
  expect(params).not.toHaveProperty('minPrice')
  expect(params).not.toHaveProperty('maxPrice')
})

// AC-6: Form re-validates immediately on date change
test('AC-6: shows validation error immediately when date change makes range invalid', async () => {
  const user = userEvent.setup()
  renderForm()

  // Set a valid range first
  await user.type(screen.getByLabelText('Check-in'), '2026-07-01')
  await user.type(screen.getByLabelText('Check-out'), '2026-07-05')
  expect(screen.queryByRole('alert')).not.toBeInTheDocument()

  // Change check-in to after check-out — error should appear without submitting
  await user.clear(screen.getByLabelText('Check-in'))
  await user.type(screen.getByLabelText('Check-in'), '2026-07-10')

  expect(screen.getByRole('alert')).toBeInTheDocument()
})
