import { apiFetchJson, ApiError } from "@shared/api/client";
import { useAuthStore } from "@shared/store/auth.store";

export async function apiFetchJsonAuth<T>(path: string, init: RequestInit = {}): Promise<T> {
  try {
    return await apiFetchJson<T>(path, init);
  } catch (e) {
    if (e instanceof ApiError && e.status === 401) {
      useAuthStore.getState().clear();
    }
    throw e;
  }
}
