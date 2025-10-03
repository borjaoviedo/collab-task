import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { RouteErrorPage } from '../RouteErrorPage'

describe('RouteErrorPage', () => {
  it('renders fallback UI', () => {
    render(
      <MemoryRouter>
        <Routes>
          <Route path="/" element={<RouteErrorPage />} />
        </Routes>
      </MemoryRouter>
    )
    expect(screen.getByRole('heading', { level: 1 })).toBeInTheDocument()
  })
})
