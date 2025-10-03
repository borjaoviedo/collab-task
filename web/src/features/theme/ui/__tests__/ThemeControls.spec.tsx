import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'

function createMql(query: string, matches = false): MediaQueryList {
  return {
    matches,
    media: query,
    onchange: null,
    addEventListener: () => undefined,
    removeEventListener: () => undefined,
    addListener: () => undefined,
    removeListener: () => undefined,
    dispatchEvent: () => false,
  } as MediaQueryList
}

describe('ThemeControls', () => {
  it('renders and can switch to dark mode', async () => {
    vi.stubGlobal('matchMedia', (q: string) => createMql(q, false))
    const { ThemeControls } = await import('../ThemeControls')

    render(<ThemeControls />)

    const darkBtn = screen.getByRole('button', { name: /dark mode/i })
    fireEvent.click(darkBtn)
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark')
  })
})
