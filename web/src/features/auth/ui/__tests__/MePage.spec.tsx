import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'

type AuthSlice = {
  isAuthenticated: boolean
  profile: {
    id: string
    email: string
    role: 'User' | 'Admin'
    projectMembershipsCount?: number
    createdAt?: string
    updatedAt?: string
  } | null
}

beforeEach(() => {
  vi.resetModules()
})

describe('MePage', () => {
  it('shows loading when authenticated without cached profile', async () => {
    vi.doMock('@shared/store/auth.store', () => ({
      useAuthStore: (sel: (s: AuthSlice) => unknown) => sel({ isAuthenticated: true, profile: null }),
    }))
    const fetchMeMock = vi.fn().mockResolvedValue({
      id: 'u1',
      email: 'u1@ct.dev',
      role: 'User',
    })
    vi.doMock('@features/auth/application/auth.usecases', () => ({ fetchMe: fetchMeMock }))

    const { MePage } = await import('../MePage')

    render(
      <MemoryRouter>
        <MePage />
      </MemoryRouter>
    )

    expect(screen.getByText(/loading profile…/i)).toBeInTheDocument()

    await waitFor(() => expect(screen.queryByText(/loading profile…/i)).not.toBeInTheDocument())
    expect(screen.getByRole('heading', { name: /my profile/i })).toBeInTheDocument()
    expect(fetchMeMock).toHaveBeenCalledTimes(1)
  })

  it('renders error UI when fetch fails and useApiError maps it', async () => {
    vi.doMock('@shared/store/auth.store', () => ({
      useAuthStore: (sel: (s: AuthSlice) => unknown) => sel({ isAuthenticated: true, profile: null }),
    }))
    vi.doMock('@features/auth/application/auth.usecases', () => ({
      fetchMe: vi.fn().mockRejectedValue(new Error('boom')),
    }))
    vi.doMock('@shared/hooks/useApiError', () => ({
      useApiError: () => ({ title: 'Failed to load', message: 'Try again later' }),
    }))

    const { MePage } = await import('../MePage')

    render(
      <MemoryRouter>
        <MePage />
      </MemoryRouter>
    )

    await waitFor(() =>
      expect(screen.getByRole('alert')).toBeInTheDocument()
    )
    expect(screen.getByRole('heading', { name: /failed to load/i })).toBeInTheDocument()
    expect(screen.getByText(/try again later/i)).toBeInTheDocument()
  })

  it('renders from cached profile without fetching', async () => {
    const cached = {
      id: 'u99',
      email: 'cached@ct.dev',
      role: 'Admin' as const,
      projectMembershipsCount: 7,
      createdAt: '2024-01-01T10:00:00Z',
      updatedAt: '2024-02-01T12:00:00Z',
    }
    const fetchMeSpy = vi.fn()

    vi.doMock('@shared/store/auth.store', () => ({
      useAuthStore: (sel: (s: AuthSlice) => unknown) => sel({ isAuthenticated: true, profile: cached }),
    }))
    vi.doMock('@features/auth/application/auth.usecases', () => ({ fetchMe: fetchMeSpy }))
    vi.doMock('@shared/hooks/useApiError', async () => {
      const actual = await vi.importActual<typeof import('@shared/hooks/useApiError')>(
        '@shared/hooks/useApiError'
      )
      return actual
    })

    const { MePage } = await import('../MePage')

    render(
      <MemoryRouter>
        <MePage />
      </MemoryRouter>
    )

    expect(screen.queryByText(/loading profile…/i)).not.toBeInTheDocument()
    expect(screen.getByRole('heading', { name: /my profile/i })).toBeInTheDocument()
    expect(screen.getByText('u99')).toBeInTheDocument()
    expect(screen.getByText('cached@ct.dev')).toBeInTheDocument()
    expect(screen.getByText('Admin')).toBeInTheDocument()
    expect(screen.getByText('7')).toBeInTheDocument()
    expect(fetchMeSpy).not.toHaveBeenCalled()
  })
})
