import { apiFetchJsonAuth } from "@shared/api/authClient";
import type { components } from "@shared/api/types";
import type { User, SysRole } from "../domain/User";
import { toDomain } from "../domain/User";

type UserReadDto = components["schemas"]["UserReadDto"];

const ROLE_OUT: Record<SysRole, 0 | 1> = { User: 0, Admin: 1 };

export async function getUserById(id: string): Promise<User> {
  const dto = await apiFetchJsonAuth<UserReadDto>(`/users/${encodeURIComponent(id)}`, { method: "GET" });
  return toDomain(dto);
}

export async function getUsers(): Promise<User[]> {
  const data = await apiFetchJsonAuth<UserReadDto[]>("/users", { method: "GET" });
  return data.map(toDomain);
}

export async function changeUserRole(id: string, role: SysRole, rowVersion: string): Promise<void> {
  await apiFetchJsonAuth<void>(`/users/${encodeURIComponent(id)}/role`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ role: ROLE_OUT[role], rowVersion }),
  });
}

export async function deleteUser(id: string, rowVersion: string): Promise<void> {
  await apiFetchJsonAuth<void>(`/users/${encodeURIComponent(id)}`, {
    method: "DELETE",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ rowVersion }),
  });
}

export async function renameUser(id: string, name: string, rowVersion: string): Promise<void> {
  await apiFetchJsonAuth<void>(`/users/${encodeURIComponent(id)}/name`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ name, rowVersion }),
  });
}
