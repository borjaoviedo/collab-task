import { useMemo } from "react"
import { ApiError } from "@shared/api/client"

export type UiError = {
    status?: number
    title: string
    message?: string
    details?: Record<string, string[] | string> | string
}

type ProblemDetails = {
  type?: string;
  title?: string;
  detail?: string;
  status?: number;
  instance?: string;
  extensions?: Record<string, unknown>;
  errors?: Record<string, string[]>;
}

function isRecord(x: unknown): x is Record<string, unknown> {
  return typeof x === 'object' && x !== null
}

/** Narrow: runtime check */
export function isApiError(e: unknown): e is ApiError {
  return isRecord(e) && 'status' in e && 'body' in e
}

function pickDetails(body: unknown): UiError['details'] {
  if (isRecord(body) && 'errors' in body && isRecord((body as Record<string, unknown>).errors)) {
    return (body as { errors: Record<string, string[] | string> }).errors
  }
  if (isRecord(body) && 'extensions' in body && isRecord((body as Record<string, unknown>).extensions)) {
    const ext = (body as { extensions: Record<string, unknown> }).extensions
    if ('errors' in ext && isRecord(ext.errors)) {
      return ext.errors as Record<string, string[] | string>
    }
    if ('jsonPath' in ext && typeof ext.jsonPath === 'string') {
      return { jsonPath: ext.jsonPath }
    }
  }
  return undefined
}

/** Accepts RFC7807 ProblemDetails or arbitrary JSON/text */
export function normalizeError(e: unknown): UiError {
  if (isApiError(e)) {
    const body = e.body as ProblemDetails | string | Record<string, unknown>

    const status = (isRecord(body) && 'status' in body && typeof body.status === 'number')
      ? (body.status as number)
      : e.status

    const title =
      (isRecord(body) && 'title' in body && typeof body.title === 'string'
        ? (body.title as string)
        : httpTitle(status)) ?? 'Request failed'

    const message =
      (isRecord(body) && 'detail' in body && typeof body.detail === 'string'
        ? (body.detail as string)
        : typeof body === 'string'
        ? body
        : isRecord(body)
        ? JSON.stringify(body, null, 2)
        : undefined)

    const details = pickDetails(body)

    return { status, title, message, details }
  }
  if (e instanceof Error) return { title: 'Unexpected error', message: e.message }
  if (typeof e === 'string') return { title: 'Unexpected error', message: e }
  return { title: 'Unexpected error', message: 'Unknown error' }
}

function httpTitle(status?: number): string | undefined {
  switch (status) {
    case 400: return 'Bad request'
    case 401: return 'Unauthorized'
    case 403: return 'Forbidden'
    case 404: return 'Not found'
    case 408: return 'Request timeout'
    case 409: return 'Conflict'
    case 412: return 'Precondition failed'
    case 422: return 'Validation error'
    case 500: return 'Server error'
    default:  return undefined
  }
}

/** Hook: memorizes normalized error and adds opinionated tweaks */
export function useApiError(error: unknown) {
  return useMemo(() => {
    const ui = normalizeError(error)

    if (ui.status === 401) {
      ui.title = 'Session expired'
      ui.message = ui.message ?? 'Please sign in again.'
    }

    return ui
  }, [error])
}