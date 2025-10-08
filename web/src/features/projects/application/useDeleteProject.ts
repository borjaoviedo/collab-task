import { useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetchJson } from "@shared/api/client"

type Payload = { rowVersion: string };
export function useDeleteProject(projectId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (p: Payload) => {
      await apiFetchJson(`/projects/${projectId}`, {
        method: "DELETE",
        body: JSON.stringify({ rowVersion: p.rowVersion }),
      });
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["projects"] });
    },
  });
}
