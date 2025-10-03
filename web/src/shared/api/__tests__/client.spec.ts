import { describe, it, expect, vi, beforeEach, afterAll, type Mock } from 'vitest'
import { apiFetch, apiFetchJson, ApiError, API_BASE_URL } from '../client'

const originalFetch = globalThis.fetch

function makeResponse(
  body: unknown,
  init: (ResponseInit & { json?: boolean }) = { status: 200 }
): Response {
  const headers = new Headers(init.headers)
  const status = init.status ?? 200
  const noBody = status === 204 || status === 304
  if (!noBody && init.json !== false) {
    headers.set('Content-Type', 'application/json')
  }
  const payload = noBody
    ? undefined
    : init.json === false
      ? String(body ?? '')
      : JSON.stringify(body ?? {})
  return new Response(payload as BodyInit | null | undefined, { status, headers })
}

beforeEach(() => {
  vi.restoreAllMocks()
  localStorage.clear()
  globalThis.fetch = vi.fn() as unknown as typeof fetch
})

describe('apiFetch', () => {
  it('applies base URL and merges headers', async () => {
    const f = globalThis.fetch as unknown as Mock
    f.mockResolvedValueOnce(makeResponse({ ok: true }))

    await apiFetch('/ping', { headers: { 'X-Test': '1' } })

    const [url, init] = f.mock.calls[0] as [string, RequestInit]
    expect(url).toBe(`${API_BASE_URL}/ping`)
    const h = new Headers(init.headers)
    expect(h.get('X-Test')).toBe('1')
    expect(h.get('Accept')).toBe('application/json')
  })

  it('injects Bearer token when present', async () => {
    localStorage.setItem('auth_token', 'abc')
    const f = globalThis.fetch as unknown as Mock
    f.mockResolvedValueOnce(makeResponse({ ok: true }))

    await apiFetch('/x')

    const init = (f.mock.calls[0] as [string, RequestInit])[1]
    expect(new Headers(init.headers).get('Authorization')).toBe('Bearer abc')
  })

  it('does not set Content-Type for FormData', async () => {
    const f = globalThis.fetch as unknown as Mock
    f.mockResolvedValueOnce(makeResponse({ ok: true }))

    const fd = new FormData()
    fd.append('a', '1')
    await apiFetch('/form', { method: 'POST', body: fd })

    const init = (f.mock.calls[0] as [string, RequestInit])[1]
    expect(new Headers(init.headers).has('Content-Type')).toBe(false)
  })
})

describe('apiFetchJson', () => {
  it('parses JSON on 200', async () => {
    const f = globalThis.fetch as unknown as Mock
    f.mockResolvedValueOnce(makeResponse({ ok: true }, { status: 200 }))

    const out = await apiFetchJson<{ ok: boolean }>('/ok')
    expect(out).toEqual({ ok: true })
  })

  it('handles 204 with empty body', async () => {
    const f = globalThis.fetch as unknown as Mock
    f.mockResolvedValueOnce(makeResponse('', { status: 204, json: false }))

    const out = await apiFetchJson<string>('/no-content')
    expect(out).toBe('')
  })

  it('throws ApiError on 4xx with problem details', async () => {
    const f = globalThis.fetch as unknown as Mock
    const problem = { title: 'Bad', status: 400, detail: 'Nope' }
    f.mockResolvedValueOnce(makeResponse(problem, { status: 400 }))

    await expect(apiFetchJson('/bad')).rejects.toEqual(new ApiError(400, problem))
  })

  it('throws ApiError on 5xx with text body', async () => {
    const f = globalThis.fetch as unknown as Mock
    f.mockResolvedValueOnce(makeResponse('boom', { status: 500, json: false }))

    await expect(apiFetchJson('/err')).rejects.toEqual(new ApiError(500, 'boom'))
  })
})

afterAll(() => {
  globalThis.fetch = originalFetch
})
