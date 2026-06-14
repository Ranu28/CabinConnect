import { useCallback, useEffect, useRef } from 'react'
import { useSearchParams } from 'react-router-dom'
import { CabinResultCard } from './cabin-result-card'
import { SearchForm } from './search-form'
import { buildQueryString, useCabinSearch } from './use-cabin-search'
import type { SearchParams } from './search-types'

const RESULTS_REGION_ID = 'search-results-region'

export function SearchResultsView() {
  const [searchParams, setSearchParams] = useSearchParams()
  const { status, results, totalCount, pageSize, error, search } = useCabinSearch()
  const currentParamsRef = useRef<SearchParams | null>(null)
  const currentPageRef = useRef(1)

  // AC-6 + shareability: restore search from URL on mount
  useEffect(() => {
    const checkIn = searchParams.get('checkIn')
    const checkOut = searchParams.get('checkOut')
    if (!checkIn || !checkOut) return

    const params: SearchParams = { checkIn, checkOut }
    const guests = searchParams.get('guests')
    if (guests) params.guests = parseInt(guests, 10)
    const page = parseInt(searchParams.get('page') ?? '1', 10) || 1

    currentParamsRef.current = params
    currentPageRef.current = page
    void search(params, page)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []) // intentionally mount-only — URL is source of truth on first load

  // AC-5 + AC-10: new search always resets to page 1
  const handleSearch = useCallback(
    (params: SearchParams) => {
      currentParamsRef.current = params
      currentPageRef.current = 1
      setSearchParams(buildQueryString(params, 1))
      void search(params, 1)
    },
    [search, setSearchParams],
  )

  // AC-8: page navigation calls API with new page, scrolls to top
  const handlePageChange = useCallback(
    (page: number) => {
      if (!currentParamsRef.current) return
      currentPageRef.current = page
      setSearchParams(buildQueryString(currentParamsRef.current, page))
      void search(currentParamsRef.current, page)
      window.scrollTo({ top: 0, behavior: 'smooth' })
    },
    [search, setSearchParams],
  )

  const currentPage = currentPageRef.current
  const totalPages = pageSize > 0 ? Math.ceil(totalCount / pageSize) : 0

  return (
    <div>
      <SearchForm onSearch={handleSearch} />

      {/* AC-2: loading indicator */}
      {status === 'loading' && (
        <div role="status" aria-label="Loading results">
          Loading…
        </div>
      )}

      {/* AC-4: error state — no partial results */}
      {status === 'error' && <p role="alert">{error}</p>}

      {/* AC-3: empty state */}
      {status === 'success' && results.length === 0 && (
        <p>No cabins available for these dates — try adjusting your dates or filters</p>
      )}

      {/* AC-1: result cards in API order (lowest price first) */}
      {status === 'success' && results.length > 0 && (
        <div id={RESULTS_REGION_ID}>
          <ul aria-label="Search results">
            {results.map(r => (
              <li key={r.cabinId}>
                <CabinResultCard
                  result={r}
                  onSelect={() => {
                    /* navigation to cabin detail wired up in the detail unit */
                  }}
                />
              </li>
            ))}
          </ul>

          {/* AC-7 + AC-9: pagination — shown only when multiple pages exist */}
          {totalPages > 1 && (
            <nav aria-label="Pagination">
              <button
                onClick={() => handlePageChange(currentPage - 1)}
                disabled={currentPage <= 1}
              >
                Previous
              </button>
              <span>
                Page {currentPage} of {totalPages}
              </span>
              {/* AC-9: next disabled on last page */}
              <button
                onClick={() => handlePageChange(currentPage + 1)}
                disabled={currentPage >= totalPages}
              >
                Next
              </button>
            </nav>
          )}
        </div>
      )}
    </div>
  )
}
