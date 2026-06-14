import { useState, useCallback } from 'react'
import type { CabinSearchResult, SearchParams } from './search-types'

export type SearchStatus = 'idle' | 'loading' | 'success' | 'error'

interface SearchState {
  status: SearchStatus
  results: CabinSearchResult[]
  totalCount: number
  pageSize: number
  error: string | null
}

interface UseCabinSearchReturn extends SearchState {
  search: (params: SearchParams, page: number) => Promise<void>
}

export function useCabinSearch(apiUrl = '/api/cabins/search'): UseCabinSearchReturn {
  const [state, setState] = useState<SearchState>({
    status: 'idle',
    results: [],
    totalCount: 0,
    pageSize: 20,
    error: null,
  })

  const search = useCallback(
    async (params: SearchParams, page: number) => {
      setState(s => ({ ...s, status: 'loading', error: null }))

      try {
        const qs = buildQueryString(params, page)
        const response = await fetch(`${apiUrl}?${qs}`)

        if (!response.ok) {
          setState(s => ({
            ...s,
            status: 'error',
            results: [],
            error: 'Something went wrong. Please try again.',
          }))
          return
        }

        const json = await response.json()
        const data = json.data
        setState({
          status: 'success',
          results: data.items,
          totalCount: data.totalCount,
          pageSize: data.pageSize,
          error: null,
        })
      } catch {
        setState(s => ({
          ...s,
          status: 'error',
          results: [],
          error: 'Network error. Please try again.',
        }))
      }
    },
    [apiUrl],
  )

  return { ...state, search }
}

export function buildQueryString(params: SearchParams, page: number): string {
  const qs = new URLSearchParams({
    checkIn: params.checkIn,
    checkOut: params.checkOut,
    page: String(page),
  })
  if (params.guests != null) qs.set('guests', String(params.guests))
  if (params.amenities?.length) params.amenities.forEach(a => qs.append('amenities', a))
  if (params.minPrice != null) qs.set('minPrice', String(params.minPrice))
  if (params.maxPrice != null) qs.set('maxPrice', String(params.maxPrice))
  return qs.toString()
}
