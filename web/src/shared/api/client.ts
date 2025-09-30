export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080'

export async function apiFetch(input: string, init: RequestInit = {}) {
  const token = localStorage.getItem('auth_token') ?? undefined
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')
  if (!(init.body instanceof FormData)) headers.set('Content-Type', 'application/json')
  if (token) headers.set('Authorization', `Bearer ${token}`)

  const resp = await fetch(`${API_BASE_URL}${input}`, { ...init, headers })
  if (resp.status === 401) {
    // TODO: logout + redirect to /login
  }
  return resp
}
