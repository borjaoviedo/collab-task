import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { LoginPage } from '../LoginPage'

vi.mock('@features/auth/application/auth.usecases', () => ({ login: vi.fn().mockResolvedValue(undefined) }))
vi.mock('@shared/store/auth.store', () => ({
  useAuthStore: (sel: (s: { isAuthenticated: boolean }) => boolean) => sel({ isAuthenticated: false })
}))

describe('LoginPage', () => {
  it('toggles password visibility', () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    )

    const passwordInput = document.getElementById('password') as HTMLInputElement
    const toggle = screen.getByLabelText(/show password/i) as HTMLInputElement

    expect(passwordInput.type).toBe('password')
    fireEvent.click(toggle)
    expect(passwordInput.type).toBe('text')
  })
})
