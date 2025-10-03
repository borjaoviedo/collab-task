import { describe, it, expect } from 'vitest'
import { ApiError } from '@shared/api/client'

describe('export surface smoke', () => {
  it('ApiError is constructible', () => {
    const e = new ApiError(400, { title: 'Bad' })
    expect(e.status).toBe(400)
  })
})