import { useEffect } from "react"
import { handleApiError } from "./errors"

export function useQueryErrorNotifier(error: unknown | null): void {
  useEffect(() => {
    if (error) handleApiError(error)
  }, [error])
}
