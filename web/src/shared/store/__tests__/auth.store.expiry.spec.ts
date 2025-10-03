import { describe, it, expect, beforeEach } from 'vitest'
import { useAuthStore, hydrateAuthStoreOnBoot } from '@shared/store/auth.store'

type TokenLike = {
  accessToken: string
  tokenType: string
  expiresAtUtc: string
  userId: string
  email: string
  role: 'User' | 'Admin'
}

beforeEach(() => {
  useAuthStore.setState({ token: null, profile: null, isAuthenticated: false })
  localStorage.clear()
})

describe('auth.store expiry', () => {
  it('removes expired token on hydrate', () => {
    const expired: TokenLike = {
      accessToken: 'expired',
      tokenType: 'Bearer',
      expiresAtUtc: '2000-01-01T00:00:00Z',
      userId: 'u1',
      email: 'e@x.com',
      role: 'User',
    }
    localStorage.setItem('auth_token_full', JSON.stringify(expired))

    hydrateAuthStoreOnBoot()

    const s = useAuthStore.getState()
    expect(s.isAuthenticated).toBe(false)
    expect(s.token).toBeNull()
  })
})
