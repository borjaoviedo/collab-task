import { useState } from "react";
import {
  addProjectMember,
  updateProjectMemberRole,
  removeProjectMember,
  restoreProjectMember,
  type AddMemberRequest,
  type UpdateMemberRoleRequest,
} from "../infrastructure/projectMembers.api";

type Status = "idle" | "pending" | "success" | "error";

export function useProjectMemberMutations(projectId: string) {
  const [status, setStatus] = useState<Status>("idle");
  const [error, setError] = useState<Error | null>(null);

  async function add(input: AddMemberRequest) {
    setStatus("pending"); setError(null);
    try { await addProjectMember(projectId, input); setStatus("success"); }
    catch (e: unknown) { const err = e instanceof Error ? e : new Error("Add failed"); setError(err); setStatus("error"); throw err; }
  }

  async function changeRole(userId: string, input: UpdateMemberRoleRequest) {
    setStatus("pending"); setError(null);
    try { await updateProjectMemberRole(projectId, userId, input); setStatus("success"); }
    catch (e: unknown) { const err = e instanceof Error ? e : new Error("Update role failed"); setError(err); setStatus("error"); throw err; }
  }

  async function remove(userId: string) {
    setStatus("pending"); setError(null);
    try { await removeProjectMember(projectId, userId); setStatus("success"); }
    catch (e: unknown) { const err = e instanceof Error ? e : new Error("Remove failed"); setError(err); setStatus("error"); throw err; }
  }

  async function restore(userId: string) {
    setStatus("pending"); setError(null);
    try { await restoreProjectMember(projectId, userId); setStatus("success"); }
    catch (e: unknown) { const err = e instanceof Error ? e : new Error("Restore failed"); setError(err); setStatus("error"); throw err; }
  }

  return { add, changeRole, remove, restore, status, isPending: status === "pending", error };
}
