import { useQuery } from "@tanstack/react-query";
import { qk } from "@shared/api/queryKeys";
import { useQueryErrorNotifier } from "@shared/api/useQueryErrorNotifier";
import type { Project } from "../domain/Project";
import { getProjects } from "../infrastructure/projects.api";

export function useProjects() {
  const query = useQuery({
    queryKey: qk.projects(),
    queryFn: async (): Promise<Project[]> => {
      const data = await getProjects();
      return data.map(d => ({
        id: d.id,
        name: d.name,
        slug: d.slug,
        membersCount: (d as any).membersCount ?? 0,
        currentUserRole: (d as any).currentUserRole ?? "Reader",
      }));
    },
    staleTime: 30_000,
  });

  useQueryErrorNotifier(query.isError ? (query.error as unknown) : null);
  return query;
}
