import { apiFetchJsonAuth } from "@shared/api/authClient";
import type { ProjectMember } from "../domain/ProjectMember";
import type { components } from "@shared/api/types";

type ProjectMemberReadDto = components["schemas"]["ProjectMemberReadDto"];
type ApiRole = components["schemas"]["ProjectRole"];

export interface AddMemberRequest {
  userId: string;
  role: "Admin" | "Member" | "Reader";
}

export interface UpdateMemberRoleRequest {
  role: "Owner" | "Admin" | "Member" | "Reader";
}

function normalizeRole(role: ApiRole | undefined): ProjectMember["role"] {
  if (typeof role === "string") {
    if (role === "Owner" || role === "Admin" || role === "Member" || role === "Reader") return role;
  }
  if (typeof role === "number") {
    const map: Record<number, ProjectMember["role"]> = {
      0: "Owner",
      1: "Admin",
      2: "Member",
      3: "Reader",
    };
    return map[role] ?? "Reader";
  }
  return "Reader";
}

function toDomain(dto: ProjectMemberReadDto): ProjectMember {
  return {
    userId: dto.userId ?? "",
    name: dto.userName ?? "",
    email: dto.email ?? "",
    role: normalizeRole(dto.role),
    removedAt: dto.removedAt ?? null, // satisfy required field in domain
  };
}

export async function getProjectMembers(projectId: string): Promise<ProjectMember[]> {
  const id = encodeURIComponent(projectId);
  const data = await apiFetchJsonAuth<ProjectMemberReadDto[]>(
    `/projects/${id}/members?includeRemoved=false`,
    { method: "GET" }
  );
  return data.map(toDomain);
}

export async function addProjectMember(projectId: string, payload: AddMemberRequest): Promise<void> {
  const id = encodeURIComponent(projectId);
  await apiFetchJsonAuth<void>(`/projects/${id}/members`, {
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
  const id = encodeURIComponent(projectId);
  const uid = encodeURIComponent(userId);
  await apiFetchJsonAuth<void>(`/projects/${id}/members/${uid}/role`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
}

export async function removeProjectMember(projectId: string, userId: string): Promise<void> {
  const id = encodeURIComponent(projectId);
  const uid = encodeURIComponent(userId);
  await apiFetchJsonAuth<void>(`/projects/${id}/members/${uid}/remove`, { method: "PATCH" });
}

export async function restoreProjectMember(projectId: string, userId: string): Promise<void> {
  const id = encodeURIComponent(projectId);
  const uid = encodeURIComponent(userId);
  await apiFetchJsonAuth<void>(`/projects/${id}/members/${uid}/restore`, { method: "PATCH" });
}
