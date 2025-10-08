import { toast } from "@shared/ui/toast"
import { useAuthStore } from "@shared/store/auth.store"

export function handleApiError(e: unknown): void {
  const status = (() => {
    if (typeof e === "object" && e !== null && "status" in e) {
      const s = (e as { status?: unknown }).status
      if (typeof s === "number") return s
    }
    return 0
  })()

  if (status === 401) { useAuthStore.getState().logout(); return }
  if (status === 403) { toast.error("You do not have permission to perform this action."); return }
  if (status === 404) { toast.error("Resource not found."); return }
  toast.error("Unexpected error. Please try again.")
}
