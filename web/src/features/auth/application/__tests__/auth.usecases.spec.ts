import { describe, it, expect, vi, beforeEach, type Mock } from 'vitest'

vi.mock('@shared/api/authClient', () => ({
  apiFetchJsonAuth: vi.fn()
}))

const setToken = vi.fn()
const setProfile = vi.fn()
const clear = vi.fn()

vi.mock('@shared/store/auth.store', () => ({
  useAuthStore: { getState: () => ({ setToken, setProfile, clear }) }
}))

import { apiFetchJsonAuth } from '@shared/api/authClient'
import { login, register, fetchMe, logout } from '../auth.usecases'

describe('auth.usecases', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('login sets token', async () => {
    ;(apiFetchJsonAuth as unknown as Mock).mockResolvedValueOnce({ accessToken: 't', expiresAtUtc: '2099-01-01T00:00:00Z' })
    await login({ email: 'a@b.com', password: 'Abcdef1!' })
    expect(setToken).toHaveBeenCalled()
  })

  it('register sets token', async () => {
    ;(apiFetchJsonAuth as unknown as Mock).mockResolvedValueOnce({ accessToken: 't', expiresAtUtc: '2099-01-01T00:00:00Z' })
    await register({ email: 'a@b.com', password: 'Abcdef1!' })
    expect(setToken).toHaveBeenCalled()
  })

  it('fetchMe sets profile and returns it', async () => {
    const profile = { id: 'u1', email: 'a@b.com', role: 'User' } as const
    ;(apiFetchJsonAuth as unknown as Mock).mockResolvedValueOnce(profile)
    const res = await fetchMe()
    expect(setProfile).toHaveBeenCalledWith(profile)
    expect(res).toEqual(profile)
  })

  it('logout clears store', () => {
    logout()
    expect(clear).toHaveBeenCalledTimes(1)
  })
})
