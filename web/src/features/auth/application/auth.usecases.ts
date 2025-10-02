import { apiFetchJsonAuth } from "@shared/api/authClient";
import { useAuthStore } from "@shared/store/auth.store";
import type { AuthToken, UserProfile } from "@shared/types/auth";

type LoginReq = { email: string; password: string };
type RegisterReq = { email: string; password: string };

/** Sign in and persist token */
export async function login(payload: LoginReq): Promise<void> {
  const token = await apiFetchJsonAuth<AuthToken>("/auth/login", {
    method: "POST",
    body: JSON.stringify(payload),
  });
  useAuthStore.getState().setToken(token);
}

/** Register and persist token */
export async function register(payload: RegisterReq): Promise<void> {
  const token = await apiFetchJsonAuth<AuthToken>("/auth/register", {
    method: "POST",
    body: JSON.stringify(payload),
  });
  useAuthStore.getState().setToken(token);
}

/** Fetch current profile and cache it */
export async function fetchMe(): Promise<UserProfile> {
  const me = await apiFetchJsonAuth<UserProfile>("/auth/me");
  useAuthStore.getState().setProfile(me);
  return me;
}

/** Local logout */
export function logout(): void {
  useAuthStore.getState().clear();
}
