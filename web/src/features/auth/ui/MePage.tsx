import { useEffect, useRef, useState } from "react";
import { fetchMe } from "@features/auth/application/auth.usecases";
import { useAuthStore } from "@shared/store/auth.store";
import { useApiError } from "@shared/hooks/useApiError";
import type { UserProfile } from "@shared/types/auth";

type ExtendedUserProfile = UserProfile & {
  projectMembershipsCount?: number;
  createdAt?: string;
  updatedAt?: string;
};

export function MePage() {
  const isAuth = useAuthStore((s) => s.isAuthenticated);
  const cached = useAuthStore((s) => s.profile) as ExtendedUserProfile | null;

  const [data, setData] = useState<ExtendedUserProfile | null>(cached ?? null);
  const [error, setError] = useState<unknown>(null);
  const uiError = useApiError(error);

  const hasCachedProfile = Boolean(cached?.id);
  // Do not show loading if an error exists
  const loading = isAuth && !hasCachedProfile && !data && !error;

  // Guard to avoid duplicate fetch in React StrictMode dev
  const requestedRef = useRef(false);

  useEffect(() => {
    let active = true;
    if (!isAuth || hasCachedProfile) return;
    if (requestedRef.current) return;

    requestedRef.current = true;
    setError(null);

    (async () => {
      try {
        const me = (await fetchMe()) as ExtendedUserProfile;
        if (!active) return;
        setData(me);
      } catch (e) {
        if (active) setError(e);
      }
    })();

    return () => {
      active = false;
    };
  }, [isAuth, hasCachedProfile]);

  if (loading) return <p aria-busy="true">Loading profile…</p>;

  if (isAuth && uiError) {
    return (
      <section role="alert" aria-live="polite">
        <h2 className="text-lg font-semibold">{uiError.title}</h2>
        {uiError.message && <p className="mt-2 whitespace-pre-wrap">{uiError.message}</p>}
      </section>
    );
  }

  const p = data ?? cached ?? null;

  return (
    <section aria-labelledby="me-heading" className="max-w-xl">
      <h1 id="me-heading" className="text-2xl font-bold">My profile</h1>
      <dl className="mt-4 space-y-2">
        <div><dt className="font-medium">Id</dt><dd className="break-all">{p ? p.id : "—"}</dd></div>
        <div><dt className="font-medium">Email</dt><dd>{p ? p.email : "—"}</dd></div>
        <div><dt className="font-medium">Role</dt><dd>{p ? String(p.role) : "—"}</dd></div>
        <div><dt className="font-medium">Projects memberships</dt><dd>{p?.projectMembershipsCount ?? 0}</dd></div>
        <div><dt className="font-medium">Account created</dt><dd>{p?.createdAt ? new Date(p.createdAt).toLocaleString() : "—"}</dd></div>
        <div><dt className="font-medium">Last updated</dt><dd>{p?.updatedAt ? new Date(p.updatedAt).toLocaleString() : "—"}</dd></div>
      </dl>
    </section>
  );
}
