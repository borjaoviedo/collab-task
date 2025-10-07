import { describe, it, expect, beforeEach } from 'vitest'
import { useAuthStore, hydrateAuthStoreOnBoot } from '@shared/store/auth.store'

type TokenLike = {
  accessToken: string
  tokenType: string
  expiresAtUtc: string
  userId: string
  email: string
  role?: 'User' | 'Admin'
}

type ProfileLike = {
  id: string
  email: string
  role: 0 | 1
  projectMembershipsCount: number
  name?: string | null
}

beforeEach(() => {
  useAuthStore.setState({ token: null, profile: null, isAuthenticated: false })
  localStorage.clear()
})

describe('auth.store', () => {
  it('setToken sets token and auth flag', () => {
    const token: TokenLike = {
      accessToken: 't',
      tokenType: 'Bearer',
      expiresAtUtc: '2099-01-01T00:00:00Z',
      userId: 'u1',
      email: 'a@b.com',
      role: 'User'
    }
    useAuthStore.getState().setToken(token)
    const s = useAuthStore.getState()
    expect(s.token?.accessToken).toBe('t')
    expect(s.isAuthenticated).toBe(true)
  })

  it('setProfile sets profile', () => {
    const profile: ProfileLike = {
      id: 'u1',
      email: 'a@b.com',
      role: 0, // User
      projectMembershipsCount: 0,
    }
    useAuthStore.getState().setProfile(profile)
    expect(useAuthStore.getState().profile?.email).toBe('a@b.com')
  })

  it('clear wipes state and storage', () => {
    const token: TokenLike = {
      accessToken: 'x',
      tokenType: 'Bearer',
      expiresAtUtc: '2099-01-01T00:00:00Z',
      userId: 'u1',
      email: 'x@x.com',
      role: 'User'
    }
    const profile: ProfileLike = {
      id: 'u1',
      email: 'x@x.com',
      role: 0,
      projectMembershipsCount: 3,
    }

    useAuthStore.setState({ token, profile, isAuthenticated: true })
    localStorage.setItem('auth_token', token.accessToken)
    localStorage.setItem('auth_token_full', JSON.stringify(token))

    useAuthStore.getState().clear()

    const s = useAuthStore.getState()
    expect(s.isAuthenticated).toBe(false)
    expect(s.token).toBeNull()
    expect(s.profile).toBeNull()
    expect(localStorage.getItem('auth_token')).toBeNull()
    expect(localStorage.getItem('auth_token_full')).toBeNull()
  })

  it('hydrate restores token if present and valid', () => {
    const token: TokenLike = {
      accessToken: 'k',
      tokenType: 'Bearer',
      expiresAtUtc: '2099-01-01T00:00:00Z',
      userId: 'u1',
      email: 'a@b.com',
      role: 'User',
    }
    localStorage.setItem('auth_token_full', JSON.stringify(token))

    hydrateAuthStoreOnBoot()

    const s = useAuthStore.getState()
    expect(s.token?.accessToken).toBe('k')
    expect(s.isAuthenticated).toBe(true)
  })
})