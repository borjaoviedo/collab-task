import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { RegisterPage } from '../RegisterPage'

describe('RegisterPage', () => {
  it('renders and has disabled submit initially', () => {
    render(<MemoryRouter><RegisterPage /></MemoryRouter>)
    const btn = screen.getByRole('button', { name: /create account/i })
    expect(btn).toBeDisabled()
  })
})
