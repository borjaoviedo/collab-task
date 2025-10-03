import { describe, it, expect } from 'vitest'
import { renderHook } from '@testing-library/react'
import { useApiError, type UiError } from '../../hooks/useApiError'
import { ApiError } from '../../api/client'

type RawUiError = {
  title: string
  status?: number
  detail?: string
  details?: Record<string, unknown>
}

describe('useApiError', () => {
  it('returns null for null', () => {
    const { result } = renderHook(() => useApiError(null))
    expect(result.current).toBeNull()
  })

  it('maps ApiError to UiError', () => {
    const err = new ApiError(400, { title: 'Bad', detail: 'Nope' })
    const { result } = renderHook(() => useApiError(err))
    const ui = result.current as UiError
    expect(ui.title).toBe('Bad')
    expect(ui.status).toBe(400)
  })

  it('maps detail string', () => {
  const err = new ApiError(400, { title: 'Bad', detail: 'X' })
  const { result } = renderHook(() => useApiError(err))
  const ui = result.current as RawUiError
  expect(JSON.stringify(ui)).toContain('X')
})

  it('special-cases 401', () => {
    const err = new ApiError(401, { title: 'Unauthorized' })
    const { result } = renderHook(() => useApiError(err))
    const ui = result.current as UiError
    expect(ui.title).toBe('Session expired')
  })

  it('normalizes 422 field errors', () => {
    const err = new ApiError(422, { title: 'Validation', errors: { Email: ['invalid'], Password: 'weak' } })
    const { result } = renderHook(() => useApiError(err))
    const ui = result.current as RawUiError
    expect(ui.details).toEqual({ Email: ['invalid'], Password: 'weak' })
  })

  it('returns generic title for unknown shapes', () => {
    const err = new ApiError(418, 'I am a teapot')
    const { result } = renderHook(() => useApiError(err))
    const ui = result.current as UiError
    expect(ui.title.length).toBeGreaterThan(0)
  })
})
