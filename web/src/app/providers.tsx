import type { PropsWithChildren } from "react"
import { QueryClient, QueryClientProvider, QueryCache, MutationCache } from "@tanstack/react-query"
import { handleApiError } from "@shared/api/errors"

function getStatus(e: unknown): number {
  const s = (e as { status?: unknown })?.status
  return typeof s === "number" ? s : 0
}

const client = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      gcTime: 300_000,
      retry(failureCount, error) {
        const status = getStatus(error)
        if (status >= 400 && status < 500) return false
        return failureCount < 2
      },
      refetchOnWindowFocus: false,
    },
    mutations: { retry: 0 },
  },
  queryCache: new QueryCache({ onError: handleApiError }),
  mutationCache: new MutationCache({ onError: handleApiError }),
})

export function AppProviders({ children }: PropsWithChildren) {
  return <QueryClientProvider client={client}>{children}</QueryClientProvider>
}
