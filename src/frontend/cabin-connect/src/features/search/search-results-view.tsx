import { useCallback, useEffect, useRef } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { CabinResultCard } from './cabin-result-card'
import { SearchForm } from './search-form'
import { buildQueryString, useCabinSearch } from './use-cabin-search'
import type { SearchParams } from './search-types'

const RESULTS_REGION_ID = 'search-results-region'

export function SearchResultsView() {
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()
  const { status, results, totalCount, pageSize, error, search } = useCabinSearch()
  const currentParamsRef = useRef<SearchParams | null>(null)
  const currentPageRef   = useRef(1)

  // Restore search from URL on mount (AC-6 + shareability)
  useEffect(() => {
    const checkIn  = searchParams.get('checkIn')
    const checkOut = searchParams.get('checkOut')
    if (!checkIn || !checkOut) return

    const params: SearchParams = { checkIn, checkOut }
    const guests = searchParams.get('guests')
    if (guests) params.guests = parseInt(guests, 10)
    const page = parseInt(searchParams.get('page') ?? '1', 10) || 1

    currentParamsRef.current = params
    currentPageRef.current   = page
    void search(params, page)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []) // intentionally mount-only — URL is source of truth on first load

  // AC-5 + AC-10: new search resets to page 1
  const handleSearch = useCallback(
    (params: SearchParams) => {
      currentParamsRef.current = params
      currentPageRef.current   = 1
      setSearchParams(buildQueryString(params, 1))
      void search(params, 1)
    },
    [search, setSearchParams],
  )

  // AC-8: page navigation
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
  const totalPages  = pageSize > 0 ? Math.ceil(totalCount / pageSize) : 0

  return (
    <>
      {/* Signature: forest gradient hero with search panel */}
      <section className="search-hero" aria-label="Search cabins">
        <span className="search-hero__eyebrow">Find your escape</span>
        <h1>Cabins for every adventure</h1>
        <p className="search-hero__sub">Pick your dates and discover the perfect hideaway</p>
        <div className="search-panel">
          <SearchForm onSearch={handleSearch} />
        </div>
      </section>

      {/* Results */}
      <div className="results-section" id={RESULTS_REGION_ID}>
        {status === 'loading' && (
          <div className="results-state" role="status" aria-label="Loading results">
            Loading cabins…
          </div>
        )}

        {status === 'error' && (
          <div className="results-state">
            <p role="alert" style={{ color: 'var(--error)' }}>{error}</p>
          </div>
        )}

        {status === 'success' && results.length === 0 && (
          <div className="results-state">
            <p>No cabins available for these dates — try adjusting your dates or filters.</p>
          </div>
        )}

        {status === 'success' && results.length > 0 && (
          <>
            <ul className="cabin-grid" aria-label="Search results">
              {results.map(r => (
                <li key={r.cabinId} style={{ listStyle: 'none' }}>
                  <CabinResultCard
                    result={r}
                    onSelect={cabinId =>
                      navigate(`/cabins/${cabinId}`, {
                        state: {
                          checkIn:  currentParamsRef.current?.checkIn,
                          checkOut: currentParamsRef.current?.checkOut,
                        },
                      })
                    }
                  />
                </li>
              ))}
            </ul>

            {totalPages > 1 && (
              <nav className="pagination" aria-label="Pagination">
                <button
                  className="btn btn-ghost"
                  onClick={() => handlePageChange(currentPage - 1)}
                  disabled={currentPage <= 1}
                >
                  ← Previous
                </button>
                <span className="text-muted">
                  Page {currentPage} of {totalPages}
                </span>
                <button
                  className="btn btn-ghost"
                  onClick={() => handlePageChange(currentPage + 1)}
                  disabled={currentPage >= totalPages}
                >
                  Next →
                </button>
              </nav>
            )}
          </>
        )}
      </div>
    </>
  )
}
