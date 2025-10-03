import { describe, it, expect, vi, beforeEach, type Mock } from 'vitest'

vi.mock('../client', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../client')>()
  return {
    ...actual,
    apiFetchJson: vi.fn(),
  }
})

vi.mock('../../store/auth.store', () => {
  const clear = vi.fn()
  return { useAuthStore: { getState: () => ({ clear }) } }
})

import { apiFetchJson as apiFetchJsonMock } from '../client'
import { apiFetchJsonAuth } from '../authClient'
import { ApiError } from '../client'

describe('apiFetchJsonAuth', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('returns data when ok', async () => {
    (apiFetchJsonMock as unknown as Mock).mockResolvedValueOnce({ ok: true })
    const res = await apiFetchJsonAuth<{ ok: boolean }>('/anything')
    expect(res.ok).toBe(true)
  })

  it('clears store on 401 then rethrows', async () => {
    const clear = (await import('../../store/auth.store')).useAuthStore.getState().clear as unknown as Mock
    ;(apiFetchJsonMock as unknown as Mock).mockRejectedValueOnce(new ApiError(401, { title: 'Unauthorized' }))
    await expect(apiFetchJsonAuth('/x')).rejects.toBeInstanceOf(ApiError)
    expect(clear).toHaveBeenCalledTimes(1)
  })

  it('does not clear on non-401', async () => {
    const clear = (await import('../../store/auth.store')).useAuthStore.getState().clear as unknown as Mock
    ;(apiFetchJsonMock as unknown as Mock).mockRejectedValueOnce(new ApiError(422, { title: 'Validation' }))
    await expect(apiFetchJsonAuth('/x')).rejects.toBeInstanceOf(ApiError)
    expect(clear).not.toHaveBeenCalled()
  })
})
