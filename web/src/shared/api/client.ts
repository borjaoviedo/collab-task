import { ENV } from "@shared/config/env"

export const API_BASE_URL = (ENV.API_BASE_URL ?? "http://localhost:8080").replace(/\/+$/, "")

function buildUrl(path: string): string {
  if (/^https?:\/\//i.test(path)) return path
  const p = path.startsWith("/") ? path : `/${path}`
  return `${API_BASE_URL}${p}`
}

export class ApiError extends Error {
  readonly status: number
  readonly body: unknown
  constructor(status: number, body: unknown, message?: string) {
    super(message ?? `API request failed with status ${status}`)
    this.status = status
    this.body = body
  }
}

export async function apiFetch(path: string, init: RequestInit = {}): Promise<Response> {
  const headers = new Headers(init.headers)
  headers.set("Accept", "application/json")

  const isFormData = init.body instanceof FormData
  if (!isFormData && !headers.has("Content-Type")) headers.set("Content-Type", "application/json")

  const token = localStorage.getItem("auth_token")
  if (token && !headers.has("Authorization")) headers.set("Authorization", `Bearer ${token}`)

  return fetch(buildUrl(path), { ...init, headers })
}

function extractMessage(body: unknown): string | undefined {
  if (typeof body === "object" && body !== null && "message" in body) {
    const m = (body as { message?: unknown }).message
    if (typeof m === "string") return m
  }
  return undefined
}

export async function apiFetchJson<T = unknown>(path: string, init: RequestInit = {}): Promise<T> {
  const resp = await apiFetch(path, init)
  const ct = resp.headers.get("Content-Type") ?? ""
  const body = ct.includes("application/json") ? await resp.json() : await resp.text()

  if (!resp.ok) throw new ApiError(resp.status, body, extractMessage(body))
  if (resp.status === 204) return undefined as unknown as T
  return body as T
}
