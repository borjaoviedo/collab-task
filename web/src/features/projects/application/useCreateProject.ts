import { useQueryClient, useMutation } from "@tanstack/react-query"
import { apiFetchJson, ApiError } from "@shared/api/client"
import { qk } from "@shared/api/queryKeys"
import { handleApiError } from "@shared/api/errors"
import type { Project } from "../domain/Project"

type CreateProjectCmd = { name: string }

type ProjectReadDto = {
  id: string
  name: string
  slug: string
  membersCount?: number
  currentUserRole?: Project["currentUserRole"]
}

const toProject = (d: ProjectReadDto): Project => ({
  id: d.id,
  name: d.name,
  slug: d.slug,
  membersCount: d.membersCount ?? 0,
  currentUserRole: d.currentUserRole ?? "Owner",
})

export function useCreateProject() {
  const qc = useQueryClient()
  return useMutation<Project, ApiError, CreateProjectCmd>({
    mutationFn: async (cmd) => {
      const dto = await apiFetchJson<ProjectReadDto>("/projects", {
        method: "POST",
        body: JSON.stringify(cmd),
      })
      return toProject(dto)
    },
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: qk.projects() })
    },
    onError: (err) => handleApiError(err),
  })
}
