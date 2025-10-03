import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'

type AuthSlice = { isAuthenticated: boolean; logout?: () => void }

beforeEach(() => {
  vi.resetModules()
})

describe('HomePage', () => {
  it('renders PublicHero with auth links when not authenticated', async () => {
    vi.doMock('@shared/store/auth.store', () => ({
      useAuthStore: (sel: (s: AuthSlice) => unknown) => sel({ isAuthenticated: false }),
    }))

    const { HomePage } = await import('../HomePage')

    render(
      <MemoryRouter>
        <HomePage />
      </MemoryRouter>
    )

    expect(screen.getByRole('heading', { name: /collaborate\. prioritize\. ship\./i })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /go to sign in/i })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /go to registration/i })).toBeInTheDocument()
    expect(screen.getByText(/frictionless/i)).toBeInTheDocument()
    expect(screen.getByText(/focus first/i)).toBeInTheDocument()
    expect(screen.getByText(/shared context/i)).toBeInTheDocument()
  })

  it('renders app header, quick links and triggers logout when authenticated', async () => {
    const logout = vi.fn()
    vi.doMock('@shared/store/auth.store', () => ({
      useAuthStore: (sel: (s: AuthSlice) => unknown) => sel({ isAuthenticated: true, logout }),
    }))

    const { HomePage } = await import('../HomePage')

    render(
      <MemoryRouter>
        <HomePage />
      </MemoryRouter>
    )

    expect(screen.getByRole('heading', { name: /collabtask/i })).toBeInTheDocument()
    expect(screen.getByText(/your collaborative hub/i)).toBeInTheDocument()

    expect(screen.getByRole('link', { name: /open projects/i })).toBeInTheDocument()
    const signOutBtn = screen.getByRole('button', { name: /sign out/i })
    const signOutLink = screen.getByRole('link', { name: /^sign out$/i })

    expect(screen.getByRole('link', { name: /today’s tasks/i })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /^profile$/i })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /^settings$/i })).toBeInTheDocument()

    fireEvent.click(signOutBtn)
    fireEvent.click(signOutLink)
    expect(logout).toHaveBeenCalledTimes(2)
  })
})
