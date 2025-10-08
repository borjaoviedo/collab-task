import { useEffect, useRef, useState } from "react";
import { fetchMe } from "@features/auth/application/auth.usecases";
import { useAuthStore } from "@shared/store/auth.store";
import { useApiError } from "@shared/hooks/useApiError";
import type { UserProfile } from "@shared/types/auth";
import { useUserRenameMutation } from "@features/users/application/useUserMutations";
import { useUserDetail } from "@features/users/application/useUserDetail";

import { Card } from "@shared/ui/Card";
import { Input } from "@shared/ui/Input";
import { Button } from "@shared/ui/Button";
import { Label } from "@shared/ui/Label";
import { FormErrorText } from "@shared/ui/FormErrorText";

export function MePage() {
  const isAuth = useAuthStore((s) => s.isAuthenticated);
  const cached = useAuthStore((s) => s.profile) as UserProfile | null;

  const [data, setData] = useState<UserProfile | null>(cached ?? null);
  const [error, setError] = useState<unknown>(null);
  const uiError = useApiError(error);

  const hasCachedProfile = Boolean(cached?.id);
  const loading = isAuth && !hasCachedProfile && !data && !error;
  const requestedRef = useRef(false);

  const profile = data ?? cached ?? null;
  const userId = profile?.id ?? "";

  const { data: userDetail } = useUserDetail(userId);
  const rowVersion = userDetail?.rowVersion ?? "";

  const [newName, setNewName] = useState<string>(profile?.name ?? "");
  const renameMut = useUserRenameMutation(userId, rowVersion);

  useEffect(() => {
    let active = true;
    if (!isAuth || hasCachedProfile) return;
    if (requestedRef.current) return;

    requestedRef.current = true;
    setError(null);
    (async () => {
      try {
        const me = (await fetchMe()) as UserProfile;
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

  useEffect(() => {
    if (profile?.name) setNewName(profile.name);
  }, [profile?.name]);

  if (loading) return <p aria-busy="true">Loading profile…</p>;
  if (isAuth && uiError) {
    return (
      <section role="alert" aria-live="polite" className="max-w-xl">
        <Card className="p-4">
          <h2 className="text-lg font-semibold">{uiError.title}</h2>
          {uiError.message && (
            <p className="mt-2 whitespace-pre-wrap">{uiError.message}</p>
          )}
        </Card>
      </section>
    );
  }

  const canSave =
    Boolean(userId) &&
    Boolean(rowVersion) &&
    newName.trim().length > 0 &&
    newName.trim() !== (profile?.name ?? "");

  return (
    <section aria-labelledby="me-heading" className="max-w-xl space-y-2">
      <h1 id="me-heading" className="text-2xl font-bold">My profile</h1>

      {/* User info */}
      <Card className="p-4">
        <dl className="grid grid-cols-[200px_1fr] gap-y-2">
          <div className="contents">
            <dt className="font-medium text-slate-700">Name</dt>
            <dd>{profile ? profile.name : "—"}</dd>
          </div>
          <div className="contents">
            <dt className="font-medium text-slate-700">Email</dt>
            <dd>{profile ? profile.email : "—"}</dd>
          </div>
          <div className="contents">
            <dt className="font-medium text-slate-700">Role</dt>
            <dd>{profile ? String(profile.role) : "—"}</dd>
          </div>
          <div className="contents">
            <dt className="font-medium text-slate-700">Projects memberships</dt>
            <dd>{profile?.projectMembershipsCount ?? 0}</dd>
          </div>
        </dl>
      </Card>

      {/* Change user name */}
      <Card className="space-y-3 p-4">
        <div className="space-y-2">
          <Label htmlFor="me-newname">Change user name</Label>
          <Input
            id="me-newname"
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
            disabled={renameMut.isPending}
          />
        </div>
        <div className="mt-1 flex gap-2">
          <Button
            onClick={() => {
              if (!canSave) return;
              const submitted = newName.trim();
              renameMut.mutate(submitted, {
                onSuccess: () => {
                  setData((prev) => (prev ? { ...prev, name: submitted } : prev));
                  renameMut.reset(); // evita quedar en isSuccess
                },
              });
            }}
            disabled={!canSave || renameMut.isPending}
            aria-busy={renameMut.isPending}
          >
            Save name
          </Button>
        </div>
        {renameMut.error && <FormErrorText>{String(renameMut.error)}</FormErrorText>}
        {renameMut.isSuccess && (
          <p role="status" aria-live="polite" className="text-sm text-emerald-700">
            Name updated
          </p>
        )}
      </Card>
    </section>
  );
}
