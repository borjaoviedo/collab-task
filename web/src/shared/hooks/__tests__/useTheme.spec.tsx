import { describe, it, expect, beforeEach, vi } from 'vitest'
import { renderHook, act } from '@testing-library/react'

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

beforeEach(() => {
  document.documentElement.removeAttribute('data-theme')
  document.documentElement.removeAttribute('data-palette')
  localStorage.clear()
  vi.stubGlobal('matchMedia', (q: string) => createMql(q, false))
})

describe('useTheme', () => {
  it('sets attributes and persists', async () => {
    const { useTheme } = await import('@shared/hooks/useTheme') 
    const { result } = renderHook(() => useTheme())

    expect(document.documentElement.getAttribute('data-theme')).toBe('light')

    act(() => result.current.setThemeMode('dark'))
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark')

    act(() => result.current.setThemePalette('warm'))
    expect(document.documentElement.getAttribute('data-palette')).toBe('warm')

    expect(localStorage.getItem('ui_theme_mode')).toBe('dark')
    expect(localStorage.getItem('ui_theme_palette')).toBe('warm')
  })
})
