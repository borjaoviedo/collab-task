import { useQuery } from "@tanstack/react-query"
import { apiFetchJson } from "@shared/api/client"
import { qk } from "@shared/api/queryKeys"
import { useQueryErrorNotifier } from "@shared/api/useQueryErrorNotifier"
import type { Project } from "../domain/Project"

type ProjectDto = {
  id: string
  name: string
  slug: string
  membersCount?: number
  currentUserRole?: Project["currentUserRole"]
}

const toProject = (d: ProjectDto): Project => ({
  id: d.id,
  name: d.name,
  slug: d.slug,
  membersCount: d.membersCount ?? 0,
  currentUserRole: d.currentUserRole ?? "Reader",
})

export function useProjects() {
  const query = useQuery({
    queryKey: qk.projects(),
    queryFn: async () => (await apiFetchJson<ProjectDto[]>("/projects")).map(toProject),
    staleTime: 30_000,
  })

  useQueryErrorNotifier(query.isError ? (query.error as unknown) : null)
  return query
}
