import { useParams } from "react-router-dom";
import { useGetProject } from "../application/useGetProject";
import { isProjectAdmin } from "../domain/Project";
import { useGetProjectMembers } from "../application/useGetProjectMembers";
import { useProjectMemberMutations } from "../application/useProjectMemberMutations";
import { Card } from "@shared/ui/Card";
import { Button } from "@shared/ui/Button";
import { Input } from "@shared/ui/Input";
import { Label } from "@shared/ui/Label";
import { FormErrorText } from "@shared/ui/FormErrorText";
import { useMemo, useState } from "react";

export default function ProjectMembersPage() {
  const { id } = useParams<{ id: string }>();
  const { data: project, isLoading: loadingProject, isError: errorProject, error: projErr } = useGetProject(id);
  const { data: members, isLoading: loadingMembers, isError: errorMembers, error: memErr, refetch } = useGetProjectMembers(id);
  const canManage = useMemo(() => project ? isProjectAdmin(project.currentUserRole) : false, [project]);

  const { add, changeRole, remove, restore, isPending, error } = useProjectMemberMutations(id ?? "");

  if (!id) {
    return <div className="w-full max-w-5xl px-6 py-8"><p className="text-red-600">Invalid route. Missing project id.</p></div>;
  }

  if (loadingProject || loadingMembers) {
    return <div className="w-full max-w-5xl px-6 py-8"><p>Loading…</p></div>;
  }

  if (errorProject || errorMembers || !project || !members) {
    const msg = projErr?.message ?? memErr?.message ?? "Failed to load project members.";
    return <div className="w-full max-w-5xl px-6 py-8"><p className="text-red-600">{msg}</p></div>;
  }

  return (
    <div className="w-full max-w-5xl px-6 py-8 text-[color:var(--color-foreground)]">
      <header className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">{project.name}</h1>
          <p className="text-sm text-[color:var(--color-muted-foreground)]">Project members</p>
        </div>
        <Button variant="outline" onClick={refetch}>Refresh</Button>
      </header>

      {canManage && (
        <AddMemberCard
          onAdd={async (userId, role) => { await add({ userId, role }); refetch(); }}
          isBusy={isPending}
          error={error}
        />
      )}

      <MembersTable
        members={members}
        canManage={canManage}
        onChangeRole={async (u, r) => { await changeRole(u, { role: r }); refetch(); }}
        onRemove={async (u) => { await remove(u); refetch(); }}
        onRestore={async (u) => { await restore(u); refetch(); }}
      />
    </div>
  );
}

function AddMemberCard({ onAdd, isBusy, error }: {
  onAdd: (userId: string, role: "Admin" | "Member" | "Reader") => Promise<void>;
  isBusy: boolean;
  error: Error | null;
}) {
  const [userId, setUserId] = useState("");
  const [role, setRole] = useState<"Admin" | "Member" | "Reader">("Member");
  const isValid = userId.trim().length > 0;

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!isValid) return;
    await onAdd(userId.trim(), role);
    setUserId("");
    setRole("Member");
  }

  return (
    <Card className="p-4 mb-6 bg-white/70">
      <form onSubmit={handleSubmit} className="flex flex-col sm:flex-row gap-3 sm:items-end">
        <div className="flex-1 min-w-56">
          <Label htmlFor="userId">User ID</Label>
          <Input id="userId" value={userId} onChange={(e) => setUserId(e.target.value)} disabled={isBusy} />
        </div>
        <div>
          <Label htmlFor="role">Role</Label>
          <select
            id="role"
            value={role}
            onChange={(e) => setRole(e.target.value as "Admin" | "Member" | "Reader")}
            disabled={isBusy}
            className="h-10 px-3 rounded-md border bg-white/70 text-black"
          >
            <option value="Admin">Admin</option>
            <option value="Member">Member</option>
            <option value="Reader">Reader</option>
          </select>
        </div>
        <Button type="submit" disabled={!isValid || isBusy}>Add member</Button>
        {error && <FormErrorText>{error.message}</FormErrorText>}
      </form>
    </Card>
  );
}

function MembersTable({ members, canManage, onChangeRole, onRemove, onRestore }: {
  members: { userId: string; name: string; email: string; role: "Owner" | "Admin" | "Member" | "Reader"; removedAt: string | null }[];
  canManage: boolean;
  onChangeRole: (userId: string, role: "Owner" | "Admin" | "Member" | "Reader") => Promise<void>;
  onRemove: (userId: string) => Promise<void>;
  onRestore: (userId: string) => Promise<void>;
}) {
  return (
    <Card className="p-0 overflow-hidden">
      <table className="w-full text-sm">
        <thead className="bg-black/5">
          <tr>
            <th className="text-left px-4 py-3">Name</th>
            <th className="text-left px-4 py-3">Email</th>
            <th className="text-left px-4 py-3">Role</th>
            <th className="text-left px-4 py-3">Status</th>
            {canManage && <th className="text-right px-4 py-3">Actions</th>}
          </tr>
        </thead>
        <tbody>
          {members.map((m) => (
            <tr key={m.userId} className="border-t">
              <td className="px-4 py-3">{m.name}</td>
              <td className="px-4 py-3">{m.email}</td>
              <td className="px-4 py-3">
                {canManage ? (
                  <select
                    value={m.role}
                    onChange={(e) => onChangeRole(m.userId, e.target.value as "Owner" | "Admin" | "Member" | "Reader")}
                    className="h-9 px-3 rounded-md border bg-white/70 text-black"
                  >
                    <option value="Owner" disabled>Owner</option>
                    <option value="Admin">Admin</option>
                    <option value="Member">Member</option>
                    <option value="Reader">Reader</option>
                  </select>
                ) : (
                  m.role
                )}
              </td>
              <td className="px-4 py-3">{m.removedAt ? "Removed" : "Active"}</td>
              {canManage && (
                <td className="px-4 py-3 text-right">
                  {m.removedAt ? (
                    <Button variant="outline" onClick={() => onRestore(m.userId)}>Restore</Button>
                  ) : (
                    <Button variant="outline" onClick={() => onRemove(m.userId)}>Remove</Button>
                  )}
                </td>
              )}
            </tr>
          ))}
        </tbody>
      </table>
    </Card>
  );
}
