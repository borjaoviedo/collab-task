import { useEffect, useState } from "react";
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
  const cached = useAuthStore((s) => s.profile);
  const [data, setData] = useState<ExtendedUserProfile  | null>(cached as ExtendedUserProfile | null);
  const [loading, setLoading] = useState<boolean>(!cached);
  const [error, setError] = useState<unknown>(null);
  const uiError = useApiError(error);

  useEffect(() => {
    let active = true;
    if (cached) return; // already in store
    (async () => {
      try {
        setLoading(true);
        const me = (await fetchMe()) as ExtendedUserProfile;
        if (active) setData(me);
      } catch (e) {
        if (active) setError(e);
      } finally {
        if (active) setLoading(false);
      }
    })();
    return () => {
      active = false;
    };
  }, [cached]);

  if (loading) return <p aria-busy="true">Loading profile…</p>;
  if (uiError.title) {
    return (
      <section role="alert" aria-live="polite">
        <h2 className="text-lg font-semibold">{uiError.title}</h2>
        {uiError.message && <p className="mt-2 whitespace-pre-wrap">{uiError.message}</p>}
      </section>
    );
  }

  const p = data ?? (cached as ExtendedUserProfile);
  return (
    <section aria-labelledby="me-heading" className="max-w-xl">
      <h1 id="me-heading" className="text-2xl font-bold">My profile</h1>
      <dl className="mt-4 space-y-2">
        <div>
          <dt className="font-medium">Id</dt>
          <dd className="break-all">{p.id}</dd>
        </div>
        <div>
          <dt className="font-medium">Email</dt>
          <dd>{p.email}</dd>
        </div>
        <div>
          <dt className="font-medium">Role</dt>
          <dd>{String(p.role)}</dd>
        </div>
        {"projectMembershipsCount" in p && (
          <div>
            <dt className="font-medium">Projects</dt>
            <dd>{(p as UserProfile & { projectMembershipsCount?: number }).projectMembershipsCount ?? 0}</dd>
          </div>
        )}
        <div>
          <dt className="font-medium">Created</dt>
          <dd>{("createdAt" in p ? new Date(p.createdAt).toLocaleString() : "—")}</dd>
        </div>
        <div>
          <dt className="font-medium">Updated</dt>
          <dd>{("updatedAt" in p ? new Date(p.updatedAt).toLocaleString() : "—")}</dd>
        </div>
      </dl>
    </section>
  );
}
