import { describe, it, expect, vi } from 'vitest'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { render, screen } from '@testing-library/react'

vi.mock('@shared/store/auth.store', () => ({
  useAuthStore: (sel: (s: { isAuthenticated: boolean }) => boolean) => sel({ isAuthenticated: false })
}))

import { AuthGuard } from '../AuthGuard'

function renderWithRouter(initial: string) {
  return render(
    <MemoryRouter initialEntries={[initial]}>
      <Routes>
        <Route element={<AuthGuard />}>
          <Route path="/private" element={<div>Private</div>} />
        </Route>
        <Route path="/login" element={<div>Login</div>} />
      </Routes>
    </MemoryRouter>
  )
}

describe('AuthGuard', () => {
  it('redirects to /login when not authenticated', () => {
    renderWithRouter('/private')
    expect(screen.getByText('Login')).toBeInTheDocument()
  })
})
