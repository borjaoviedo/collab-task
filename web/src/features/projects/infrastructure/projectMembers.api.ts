import { apiFetchJsonAuth } from "@shared/api/authClient";
import type { ProjectMember } from "../domain/ProjectMember";

export interface AddMemberRequest {
  userId: string;
  role: "Admin" | "Member" | "Reader";
}

export interface UpdateMemberRoleRequest {
  role: "Owner" | "Admin" | "Member" | "Reader";
}

export async function getProjectMembers(projectId: string): Promise<ProjectMember[]> {
  return apiFetchJsonAuth<ProjectMember[]>(`/projects/${projectId}/members`, { method: "GET" });
}

export async function addProjectMember(projectId: string, payload: AddMemberRequest): Promise<void> {
  await apiFetchJsonAuth<void>(`/projects/${projectId}/members`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
}

export async function updateProjectMemberRole(
  projectId: string,
  userId: string,
  payload: UpdateMemberRoleRequest
): Promise<void> {
  await apiFetchJsonAuth<void>(`/projects/${projectId}/members/${userId}/role`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
}

export async function removeProjectMember(projectId: string, userId: string): Promise<void> {
  await apiFetchJsonAuth<void>(`/projects/${projectId}/members/${userId}/remove`, { method: "PATCH" });
}

export async function restoreProjectMember(projectId: string, userId: string): Promise<void> {
  await apiFetchJsonAuth<void>(`/projects/${projectId}/members/${userId}/restore`, { method: "PATCH" });
}
