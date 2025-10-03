import { describe, it, expect } from 'vitest'
import { isValidEmail, passwordError, normalizeServerFieldErrors } from '../auth'

describe('isValidEmail', () => {
  it('accepts valid email', () => {
    expect(isValidEmail('a@b.com')).toBe(true)
  })
  it('rejects invalid email', () => {
    expect(isValidEmail('no-at-domain')).toBe(false)
  })
})

describe('passwordError', () => {
  it('requires length', () => {
    expect(passwordError('Ab1!')).toContain('at least 8')
  })
  it('requires uppercase', () => {
    expect(passwordError('abcd123!')).toContain('uppercase')
  })
  it('requires number', () => {
    expect(passwordError('Abcdefg!')).toContain('number')
  })
  it('requires special char', () => {
    expect(passwordError('Abcdefg1')).toContain('special')
  })
  it('returns null when strong', () => {
    expect(passwordError('Abcdef1!')).toBeNull()
  })
})

describe('normalizeServerFieldErrors', () => {
  it('returns empty for non-422', () => {
    expect(normalizeServerFieldErrors({ status: 400, title: 'Bad' })).toEqual({})
  })
  it('lowercases keys and wraps strings', () => {
    const ui = { status: 422, title: 'Validation', details: { Email: 'bad', Password: ['weak'] } }
    expect(normalizeServerFieldErrors(ui)).toEqual({ email: ['bad'], password: ['weak'] })
  })
})
