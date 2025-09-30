import { ENV } from "@shared/config/env"

export const API_BASE_URL = (ENV.API_BASE_URL ?? 'http://localhost:8080').replace(/\/+$/, '')

function buildUrl(path: string): string {
  if (/^https?:\/\//i.test(path)) return path
  const p = path.startsWith('/') ? path : `/${path}`
  return `${API_BASE_URL}${p}`
}

/**
 * Minimal HTTP wrapper:
 * - Applies base URL
 * - Sets common headers
 * - Injects Bearer token from localStorage ('auth_token')
 * - Returns raw Response (no JSON parsing, no side effects)
 */
export async function apiFetch(path: string, init: RequestInit = {}): Promise<Response> {
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')

  const isFormData = init.body instanceof FormData
  if (!isFormData && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const token = localStorage.getItem('auth_token')
  if (token && !headers.has('Authorization')) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  return fetch(buildUrl(path), { ...init, headers })
}

export class ApiError extends Error {
  status: number
  body: unknown

  constructor(status: number, body: unknown) {
    super(`API request failed with status ${status}`)
    this.status = status
    this.body = body
  }
}

/**
 * apiFetchJson: fetch + parse JSON + throw ApiError on non-2xx
 */
export async function apiFetchJson<T = unknown>(
  path: string,
  init: RequestInit = {}
): Promise<T> {
  const resp = await apiFetch(path, init)
  const contentType = resp.headers.get('Content-Type') ?? ''
  const body = contentType.includes('application/json') ? await resp.json() : await resp.text()

  if (!resp.ok) {
    throw new ApiError(resp.status, body)
  }
  return body as T
}