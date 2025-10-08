import { useMemo, useState } from "react";
import { useUsers } from "../application/useUsers";
import { changeUserRole, deleteUser } from "../infrastructure/users.api";
import type { User, SysRole } from "../domain/User";
import { Card } from "@shared/ui/Card";
import { Button } from "@shared/ui/Button";
import { Label } from "@shared/ui/Label";
import { Select } from "@shared/ui/Select";
import { FormErrorText } from "@shared/ui/FormErrorText";
import { useMutation, useQueryClient } from "@tanstack/react-query";

const SYS_ROLES: SysRole[] = ["User", "Admin"];

function initials(name?: string | null) {
  if (!name) return "?";
  const parts = name.trim().split(/\s+/);
  return ((parts[0]?.[0] ?? "") + (parts[parts.length - 1]?.[0] ?? "")).toUpperCase() || "?";
}
function roleBadgeClass(role: SysRole) {
  return role === "Admin" ? "bg-amber-100 text-amber-800" : "bg-slate-100 text-slate-800";
}

export default function UsersPage() {
  const { data, isLoading, isError, error } = useUsers();
  const users = useMemo(() => data ?? [], [data]);
  const [draftRole, setDraftRole] = useState<Record<string, SysRole>>({});

  const qc = useQueryClient();
  const roleMut = useMutation({
    mutationFn: (p: { id: string; role: SysRole; rowVersion: string }) =>
      changeUserRole(p.id, p.role, p.rowVersion),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["users", "all"] }),
  });
  const delMut = useMutation({
    mutationFn: (p: { id: string; rowVersion: string }) => deleteUser(p.id, p.rowVersion),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["users", "all"] }),
  });

  if (isLoading) return <p aria-busy="true">Loading users…</p>;
  if (isError) return <FormErrorText>{String(error)}</FormErrorText>;
  if (!users.length) return <p>No users found.</p>;

  return (
    <div className="space-y-4">
      <h1 className="text-xl font-semibold">All users</h1>
      <ul className="space-y-3">
        {users.map((u: User) => {
          const key = String(u.id);
          const selected: SysRole = draftRole[key] ?? u.role;
          const dirty = selected !== u.role;

          return (
            <li key={key}>
              <Card className="p-4">
                <div className="grid grid-cols-1 md:grid-cols-[auto_1fr_auto] items-center gap-4">
                  <div className="flex items-center">
                    <div className="h-10 w-10 rounded-full bg-slate-200 flex items-center justify-center text-sm font-semibold">
                      {initials(u.name)}
                    </div>
                  </div>

                  <div className="min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="font-medium truncate">{u.name || "(no name)"}</span>
                      <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs ${roleBadgeClass(u.role)}`}>
                        {u.role}
                      </span>
                    </div>
                    <div className="text-sm text-slate-600 truncate">
                      {u.email} · {new Date(u.createdAt).toLocaleDateString()} • {u.projectMembershipsCount} memberships
                    </div>
                  </div>

                  <div className="flex items-center gap-2 justify-start md:justify-end">
                    <Label htmlFor={`role-${key}`} className="sr-only">Role</Label>
                    <Select
                      id={`role-${key}`}
                      value={selected}
                      onChange={(e) => setDraftRole((d) => ({ ...d, [key]: e.target.value as SysRole }))}
                      disabled={roleMut.isPending}
                    >
                      {SYS_ROLES.map(r => (<option key={r} value={r}>{r}</option>))}
                    </Select>
                    <Button
                      onClick={() => roleMut.mutate({ id: key, role: selected, rowVersion: u.rowVersion })}
                      disabled={!dirty || roleMut.isPending}
                      aria-busy={roleMut.isPending}
                      className="h-10"
                    >
                      Save role
                    </Button>
                    <Button
                      onClick={() => delMut.mutate({ id: key, rowVersion: u.rowVersion })}
                      disabled={delMut.isPending}
                      aria-busy={delMut.isPending}
                      className="h-10"
                    >
                      Delete
                    </Button>
                  </div>
                </div>
              </Card>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
