import { useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { useProjectMembers } from "../application/useProjectMembers";
import { useProjectMemberMutations } from "../application/useProjectMemberMutations";
import { useAllUsers } from "@features/users/application/useAllUsers";
import { useGetProject } from "@features/projects/application/useGetProject";
import { isProjectAdmin, isProjectOwner } from "@features/projects/domain/Project";
import { isSystemAdmin, normalizeSysRole } from "@features/users/domain/User";
import { useAuthStore } from "@shared/store/auth.store";

import { Label } from "@shared/ui/Label";
import { Button } from "@shared/ui/Button";
import { Card } from "@shared/ui/Card";
import { FormErrorText } from "@shared/ui/FormErrorText";
import { Select } from "@shared/ui/Select";

type Role = "Owner" | "Admin" | "Member" | "Reader";
type AddRole = Exclude<Role, "Owner">;

const ROLES: Role[] = ["Owner", "Admin", "Member", "Reader"];
const ROLES_ADD: AddRole[] = ["Admin", "Member", "Reader"];

const GUID_RE =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

type WithMessage = { message: string };
function hasMessage(x: unknown): x is WithMessage {
  return typeof x === "object" && x !== null && "message" in x && typeof (x as { message?: unknown }).message === "string";
}
function toMessage(e: unknown): string {
  if (e instanceof Error) return e.message;
  if (typeof e === "string") return e;
  if (hasMessage(e)) return e.message;
  return "Action failed";
}
function initialsFromName(name: string | undefined): string {
  if (!name) return "?";
  const parts = name.trim().split(/\s+/);
  const a = parts[0]?.[0] ?? "";
  const b = parts.length > 1 ? parts[parts.length - 1][0] ?? "" : "";
  return (a + b).toUpperCase() || "?";
}
function roleBadgeClass(role: Role): string {
  switch (role) {
    case "Owner": return "bg-purple-100 text-purple-800";
    case "Admin": return "bg-amber-100 text-amber-800";
    case "Member": return "bg-sky-100 text-sky-800";
    case "Reader": return "bg-slate-100 text-slate-800";
    default: return "bg-slate-100 text-slate-800";
  }
}

export default function ProjectMembersPage() {
  const { id } = useParams<{ id: string }>();
  const invalid = !id || !GUID_RE.test(id);

  const { data, isLoading, isError, error, refetch } = useProjectMembers(id);
  const { add, changeRole, remove, restore, status, isPending, error: mError } =
    useProjectMemberMutations(id ?? "");

  // get current user role in this project
  const { data: projectData } = useGetProject(id);
  const canManage = projectData ? isProjectAdmin(projectData.currentUserRole) : false;

  const sysRoleRaw = useAuthStore((s) => s.profile?.role);
  const canListUsers = isSystemAdmin(normalizeSysRole(sysRoleRaw)); 

  const isOwner = projectData ? isProjectOwner(projectData.currentUserRole) : false;

  const { users, isLoading: usersLoading } = useAllUsers({ enabled: canListUsers });

  const members = useMemo(() => data ?? [], [data]);

  const [draftRole, setDraftRole] = useState<Record<string, Role>>({});
  const [selectedUserId, setSelectedUserId] = useState<string>("");
  const [newRole, setNewRole] = useState<AddRole>("Member");

  if (invalid) return <FormErrorText>Invalid project id.</FormErrorText>;
  if (isLoading) return <p aria-busy="true">Loading members…</p>;
  if (isError) return <FormErrorText>Failed to load members: {toMessage(error)}</FormErrorText>;

  async function handleSaveRole(userId: string) {
    const role = draftRole[userId];
    if (!role) return;
    const member = members.find(x => x.userId === userId);
    if (!member) return;
    await changeRole(userId, { role, rowVersion: member.rowVersion });
    await refetch();
  }

  async function handleRemove(userId: string) {
    const member = members.find(x => x.userId === userId);
    if (!member) return;
    await remove(userId, member.rowVersion);
    await refetch();
  }

  async function handleRestore(userId: string) {
    const member = members.find(x => x.userId === userId);
    if (!member) return;
    await restore(userId, member.rowVersion);
    await refetch();
  }

  async function handleAdd() {
    if (!GUID_RE.test(selectedUserId)) return;
    await add({ userId: selectedUserId, role: newRole });
    setSelectedUserId("");
    setNewRole("Member");
    await refetch();
  }

  return (
    <div className="space-y-8">
      {/* Add members — only Admin/Owner */}
      {canManage && (
        <div className="space-y-1">
          <h2 className="text-lg font-semibold">Add members</h2>
          <p className="text-sm text-slate-600">Select a user and assign a role to add them to this project.</p>
            <Card className="">
              <div className="grid grid-cols-1 md:grid-cols-[1fr_auto_auto_auto] items-end gap-3">
                <div className="flex flex-col">
                  <Label htmlFor="new-user">User</Label>
                  <Select
                    id="new-user"
                    value={selectedUserId}
                    onChange={(e) => setSelectedUserId(e.target.value)}
                    disabled={isPending || usersLoading}
                    aria-busy={usersLoading}
                    aria-label="Select user to add"
                  >
                    <option value="">{usersLoading ? "Loading users…" : "Select a user"}</option>
                    {users.map((u) => (
                      <option key={u.id} value={u.id}>
                        {u.name ?? "(no name)"} — {u.email ?? "(no email)"}
                      </option>
                    ))}
                  </Select>
                </div>

                <div className="flex flex-col">
                  <Label htmlFor="new-role">Role</Label>
                  <Select
                    id="new-role"
                    value={newRole}
                    onChange={(e) => setNewRole(e.target.value as AddRole)}
                    disabled={isPending}
                  >
                    {ROLES_ADD.map((r) => (
                      <option key={r} value={r}>{r}</option>
                    ))}
                  </Select>
                </div>

                <div className="flex">
                  <Button
                    type="button"
                    onClick={handleAdd}
                    disabled={isPending || !GUID_RE.test(selectedUserId)}
                    aria-busy={isPending}
                    className="h-10"
                  >
                    Add member
                  </Button>
                </div>

                {mError && <FormErrorText>{toMessage(mError)}</FormErrorText>}
              </div>

              {users.length === 0 && !usersLoading ? (
                <p className="mt-2 text-sm text-slate-500">
                  Users feature not wired yet. This dropdown will list all users once the API is connected.
                </p>
              ) : null}
            </Card>
        </div>
      )}

      {/* Members list */}
      {!members || members.length === 0 ? (
        <p>No members.</p>
      ) : (
        <ul className="space-y-1">
          <h2 className="text-lg font-semibold">Project members</h2>
          <p className="text-sm text-slate-600">View {canManage ? "and manage " : ""}the people in this project.</p>
          {members.map((m) => {
            const current = m.role as Role;
            const selected = draftRole[m.userId] ?? current;
            const dirty = selected !== current;
            const isDeleted = !!m.removedAt;

            return (
              <li key={m.userId}>
                <Card className={`p-4 ${isDeleted ? "opacity-70" : ""}`}>
                  <div className="grid grid-cols-1 md:grid-cols-[auto_1fr_auto] items-center gap-4">
                    <div className="flex items-center">
                      <div className="h-10 w-10 rounded-full bg-slate-200 flex items-center justify-center text-sm font-semibold">
                        {initialsFromName(m.name)}
                      </div>
                    </div>

                    <div className="min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className="font-medium truncate">{m.name || "(no name)"}</span>
                        <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs ${roleBadgeClass(current)}`}>
                          {current}
                        </span>
                        {isDeleted ? (
                          <span className="inline-flex items-center rounded-full px-2 py-0.5 text-xs bg-rose-100 text-rose-800">
                            removed
                          </span>
                        ) : null}
                      </div>
                      <div className="text-sm text-slate-600 truncate">
                        {m.email || "(no email)"}
                      </div>
                    </div>

                    {/* Actions — Owner for role change, Admin+Owner for remove/restore */}
                    {canManage ? (
                      <div className="flex items-center gap-2 justify-start md:justify-end">
                        <Label htmlFor={`role-${m.userId}`} className="sr-only">Role</Label>
                        <Select
                          id={`role-${m.userId}`}
                          aria-label={`role-${m.userId}`}
                          value={selected}
                          onChange={(e) => setDraftRole((d) => ({ ...d, [m.userId]: e.target.value as Role }))}
                          disabled={isPending || !isOwner}
                        >
                          {ROLES.map((r) => (<option key={r} value={r}>{r}</option>))}
                        </Select>

                        <Button
                          type="button"
                          onClick={() => handleSaveRole(m.userId)}
                          disabled={!isOwner || !dirty || isPending}
                          aria-busy={isPending && status === "pending"}
                          className="h-10"
                        >
                          Save role
                        </Button>

                        {isDeleted ? (
                          <Button type="button" onClick={() => handleRestore(m.userId)} disabled={isPending} aria-busy={isPending} className="h-10" variant="secondary">
                            Restore
                          </Button>
                        ) : (
                          <Button type="button" onClick={() => handleRemove(m.userId)} disabled={isPending} aria-busy={isPending} className="h-10">
                            Remove
                          </Button>
                        )}
                      </div>
                    ) : null}
                  </div>
                </Card>
              </li>
            );
          })}
        </ul>
      )}

      {(mError || error) && <FormErrorText>{toMessage(mError ?? error)}</FormErrorText>}
    </div>
  );
}
