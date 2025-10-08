import type { components } from "@shared/api/types";

export type UserReadDto = components["schemas"]["UserReadDto"];
type ApiUserRole = components["schemas"]["UserRole"];

export type SysRole = "User" | "Admin";

export function normalizeSysRole(role: unknown): SysRole {
  if (role === "Admin" || role === "User") return role;
  if (typeof role === "number") {
    const map: Record<number, SysRole> = { 0: "User", 1: "Admin" };
    return map[role] ?? "User";
  }
  return "User";
}

export interface User {
  id: string;
  email: string;
  name: string;
  role: SysRole;
  createdAt: string;
  updatedAt: string;
  projectMembershipsCount: number;
  rowVersion: string;
}

function roleIn(r: ApiUserRole | undefined): SysRole {
  if (typeof r === "number") return r === 1 ? "Admin" : "User";
  if (typeof r === "string") return r === "Admin" ? "Admin" : "User";
  return "User";
}

export function isSystemAdmin(role: SysRole): boolean {
  return role === "Admin";
}

export function toDomain(dto: UserReadDto): User {
  return {
    id: String(dto.id),
    email: dto.email ?? "",
    name: dto.name ?? "",
    role: roleIn(dto.role as ApiUserRole),
    createdAt: dto.createdAt ?? new Date().toISOString(),
    updatedAt: dto.updatedAt ?? new Date().toISOString(),
    projectMembershipsCount: dto.projectMembershipsCount ?? 0,
    rowVersion: (dto.rowVersion as unknown as string) ?? "",
  };
}
