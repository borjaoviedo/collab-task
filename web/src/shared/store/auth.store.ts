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
  logout: () => void;
  hydrate: () => void;
};

let logoutTimer: number | null = null;

function getExpiryUtcSeconds(t: AuthToken | null): number | null {
  if (!t) return null;
  const explicit = (t as unknown as { expiresAtUtc?: string | number | Date })?.expiresAtUtc;
  if (explicit) {
    const ms = typeof explicit === "number" ? explicit : new Date(explicit).getTime();
    return Number.isFinite(ms) ? Math.floor(ms / 1000) : null;
  }
  try {
    const parts = t.accessToken.split(".");
    if (parts.length !== 3) return null;
    const payload = JSON.parse(atob(parts[1]));
    const exp = typeof payload.exp === "number" ? payload.exp : null;
    return exp ?? null;
  } catch {
    return null;
  }
}

function isExpired(t: AuthToken | null): boolean {
  const exp = getExpiryUtcSeconds(t);
  if (!exp) return false;
  const now = Math.floor(Date.now() / 1000);
  return now >= exp;
}

function scheduleAutoLogout(clearFn: () => void, t: AuthToken | null) {
  if (logoutTimer !== null) {
    window.clearTimeout(logoutTimer);
    logoutTimer = null;
  }
  const exp = getExpiryUtcSeconds(t);
  if (!exp) return;
  const ms = exp * 1000 - Date.now();
  if (ms <= 0) {
    clearFn();
    return;
  }
  logoutTimer = window.setTimeout(clearFn, ms);
}

export const useAuthStore = create<AuthState & AuthActions>((set, get) => ({
  token: null,
  profile: null,
  isAuthenticated: false,

  setToken: (t) => {
    localStorage.setItem(TOKEN_KEY, t.accessToken);
    localStorage.setItem(TOKEN_FULL_KEY, JSON.stringify(t));

    const expired = isExpired(t);
    set({ token: expired ? null : t, isAuthenticated: !expired });
    scheduleAutoLogout(() => get().clear(), expired ? null : t);
  },

  setProfile: (p) => set({ profile: p }),

  clear: () => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(TOKEN_FULL_KEY);
    if (logoutTimer !== null) {
      window.clearTimeout(logoutTimer);
      logoutTimer = null;
    }
    set({ token: null, profile: null, isAuthenticated: false });
  },

  logout: () => {
    get().clear();
  },

  hydrate: () => {
    const raw = localStorage.getItem(TOKEN_FULL_KEY);
    if (!raw) {
      set({ token: null, isAuthenticated: false });
      return;
    }
    try {
      const t = JSON.parse(raw) as AuthToken;
      if (t?.accessToken && localStorage.getItem(TOKEN_KEY) !== t.accessToken) {
        localStorage.setItem(TOKEN_KEY, t.accessToken);
      }
      const expired = isExpired(t);
      set({ token: expired ? null : t, isAuthenticated: !expired });
      scheduleAutoLogout(() => get().clear(), expired ? null : t);
    } catch {
      set({ token: null, isAuthenticated: false });
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
  if (e.key === TOKEN_FULL_KEY) {
    if (!e.newValue) {
      s.clear();
      return;
    }
    try {
      const t = JSON.parse(e.newValue) as AuthToken;
      s.setToken(t);
    } catch {
      s.clear();
    }
  }
});

export function hydrateAuthStoreOnBoot(): void {
  useAuthStore.getState().hydrate();
}
