import { useMutation, useQueryClient } from "@tanstack/react-query";
import { changeUserRole, deleteUser, renameUser } from "../infrastructure/users.api";
import type { components } from "@shared/api/types";
import { normalizeSysRole } from "../domain/User";

type ChangeRoleDto = components["schemas"]["ChangeRoleDto"];

export function useUserRoleMutation(userId: string, rowVersion: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (role: ChangeRoleDto["role"]) => changeUserRole(userId, normalizeSysRole(role), rowVersion),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["users", "all"] }),
  });
}

export function useUserDeleteMutation(userId: string, rowVersion: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => deleteUser(userId, rowVersion),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["users", "all"] }),
  });
}

export function useUserRenameMutation(userId: string, rowVersion: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (name: string) => renameUser(userId, name, rowVersion),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["users", "all"] }),
  });
}