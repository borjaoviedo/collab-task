import { useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetchJson } from "@shared/api/client"

type Payload = { name: string; rowVersion: string };
export function useRenameProject(projectId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (p: Payload) => {
      await apiFetchJson(`/projects/${projectId}/name`, {
        method: "PATCH",
        body: JSON.stringify({ name: p.name, rowVersion: p.rowVersion }),
      });
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["project", projectId] });
      qc.invalidateQueries({ queryKey: ["projects"] });
    },
  });
}
