import { useQuery } from "@tanstack/react-query";
import { qk } from "@shared/api/queryKeys";
import { getProjectMembers } from "../infrastructure/projectMembers.api";
import type { ProjectMember } from "../domain/ProjectMember";

const UUID_RE =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-5][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$/;

export function useProjectMembers(projectId: string | undefined) {
  const enabled = typeof projectId === "string" && UUID_RE.test(projectId);

  return useQuery<ProjectMember[], Error>({
    queryKey: qk.members(enabled ? projectId! : "pending"),
    enabled,
    queryFn: () => getProjectMembers(projectId!),
    staleTime: 30_000,
    retry: 1,
  });
}