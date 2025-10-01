import { create } from "zustand";
import type { AuthToken, UserProfile } from "@shared/types/auth";

const TOKEN_KEY = "auth_token";
const TOKEN_FULL_KEY = "auth_token_full";

type AuthState = {
  token: AuthToken | null;
  profile: UserProfile | null;
  isAuthenticated: boolean;
};

type AuthActions = {
  setToken: (t: AuthToken) => void;
  setProfile: (p: UserProfile | null) => void;
  clear: () => void;
  hydrate: () => void;
};

export const useAuthStore = create<AuthState & AuthActions>((set) => ({
  token: null,
  profile: null,
  isAuthenticated: false,

  setToken: (t) => {
    localStorage.setItem(TOKEN_KEY, t.accessToken);
    localStorage.setItem(TOKEN_FULL_KEY, JSON.stringify(t));
    set({ token: t, isAuthenticated: true });
  },

  setProfile: (p) => set({ profile: p }),

  clear: () => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(TOKEN_FULL_KEY);
    set({ token: null, profile: null, isAuthenticated: false });
  },

  hydrate: () => {
    const raw = localStorage.getItem(TOKEN_FULL_KEY);
    if (!raw) return;
    try {
      const t = JSON.parse(raw) as AuthToken;
      if (t?.accessToken && localStorage.getItem(TOKEN_KEY) !== t.accessToken) {
        localStorage.setItem(TOKEN_KEY, t.accessToken);
      }
      set({ token: t, isAuthenticated: true });
    } catch {
      /* ignore */
    }
  },
}));

window.addEventListener("storage", (e) => {
  if (e.key !== TOKEN_KEY && e.key !== TOKEN_FULL_KEY) return;
  const s = useAuthStore.getState();
  if (e.key === TOKEN_KEY && e.newValue === null) {
    s.clear();
    return;
  }
  if (e.key === TOKEN_FULL_KEY && e.newValue) {
    try {
      const t = JSON.parse(e.newValue) as AuthToken;
      s.setToken(t);
    } catch {
      /* ignore */
    }
  }
});

export function hydrateAuthStoreOnBoot(): void {
  useAuthStore.getState().hydrate();
}
