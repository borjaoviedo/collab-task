import { apiFetchJsonAuth } from "@shared/api/authClient";
import type { ProjectMember } from "../domain/ProjectMember";
import type { components } from "@shared/api/types";

type ProjectMemberReadDto = components["schemas"]["ProjectMemberReadDto"];
type ApiRole = components["schemas"]["ProjectRole"];

export interface AddMemberRequest {
  userId: string;
  role: "Admin" | "Member" | "Reader";
  joinedAtUtc?: string;
}

export interface UpdateMemberRoleRequest {
  role: "Owner" | "Admin" | "Member" | "Reader";
  rowVersion: string;
}

export interface RemoveMemberRequest {
  rowVersion: string;
  removedAtUtc: string;
}

export interface RestoreMemberRequest {
  rowVersion: string;
}

// ---- role mappers ----
function normalizeRole(role: ApiRole | undefined): ProjectMember["role"] {
  if (typeof role === "string") {
    if (role === "Owner" || role === "Admin" || role === "Member" || role === "Reader") return role;
  }
  if (typeof role === "number") {
    const map: Record<number, ProjectMember["role"]> = {
      0: "Reader",
      1: "Member",
      2: "Admin",
      3: "Owner",
    };
    return map[role] ?? "Reader";
  }
  return "Reader";
}

const ROLE_OUT: Record<ProjectMember["role"], number> = {
  Reader: 0,
  Member: 1,
  Admin: 2,
  Owner: 3,
};

function toApiRoleOut(role: ProjectMember["role"]): number {
  return ROLE_OUT[role];
}

// ---- dto mappers ----
function toDomain(dto: ProjectMemberReadDto): ProjectMember {
  return {
    userId: dto.userId ?? "",
    name: dto.userName ?? "",
    email: dto.email ?? "",
    role: normalizeRole(dto.role),
    removedAt: dto.removedAt ?? null,
    rowVersion: dto.rowVersion ?? "",
  };
}

// ---- api calls ----
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
    body: JSON.stringify({
      userId: payload.userId,
      role: toApiRoleOut(payload.role as ProjectMember["role"]),
      joinedAtUtc: payload.joinedAtUtc ?? new Date().toISOString(),
    }),
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
    body: JSON.stringify({
      role: toApiRoleOut(payload.role as ProjectMember["role"]),
      rowVersion: payload.rowVersion,
    }),
  });
}

export async function removeProjectMember(
  projectId: string,
  userId: string,
  payload: RemoveMemberRequest
): Promise<void> {
  const id = encodeURIComponent(projectId);
  const uid = encodeURIComponent(userId);
  await apiFetchJsonAuth<void>(`/projects/${id}/members/${uid}/remove`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
}

export async function restoreProjectMember(
  projectId: string,
  userId: string,
  payload: RestoreMemberRequest
): Promise<void> {
  const id = encodeURIComponent(projectId);
  const uid = encodeURIComponent(userId);
  await apiFetchJsonAuth<void>(`/projects/${id}/members/${uid}/restore`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
}