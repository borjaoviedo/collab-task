import { useQueryClient, useMutation } from "@tanstack/react-query";
import { qk } from "@shared/api/queryKeys";
import { handleApiError } from "@shared/api/errors";
import { toast } from "@shared/ui/toast";
import { createProject } from "../infrastructure/projects.api";

type CreateProjectCmd = { name: string };

const UUID_RE =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-5][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$/;
const ANY_UUID_IN_TEXT =
  /[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-5][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}/;

function isValidId(s: string | null | undefined): s is string {
  if (!s) return false;
  const t = s.trim();
  return UUID_RE.test(t) || (/^\d+$/.test(t) && Number(t) > 0);
}

export function useCreateProject() {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: async ({ name }: CreateProjectCmd): Promise<string> => {
      // 1) JSON body { id }
      try {
        const json = await createProject({ name });
        const idFromJson =
          json && typeof (json as any).id === "string" ? (json as any).id : null;
        if (isValidId(idFromJson)) return idFromJson!;
      } catch (e) {
        // Reintenta con lectura manual si el backend respondió 201 sin JSON
        // o devuelve texto. Para eso, usa fetch crudo para inspeccionar headers.
      }

      // 2) Fallback crudo: inspecciona Location y/o body
      const resp = await fetch("/projects", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name }),
        credentials: "include",
      });

      if (!resp.ok && resp.status !== 201) {
        const msg = await resp.text().catch(() => "");
        const err = new Error(msg || "Create project failed") as Error & { status?: number };
        err.status = resp.status;
        throw err;
      }

      // Location
      const loc = resp.headers.get("Location");
      if (loc) {
        const id = loc.split("/").filter(Boolean).pop() ?? null;
        if (isValidId(id)) return id!;
      }

      // Body con UUID en texto
      const bodyText = await resp.text().catch(() => "");
      const m = bodyText.match(ANY_UUID_IN_TEXT);
      if (m && isValidId(m[0])) return m[0];

      const err = new Error("Cannot determine created project id") as Error & { status?: number };
      err.status = 0;
      throw err;
    },
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: qk.projects() });
      toast.success("Project created.");
    },
    onError: (e) => handleApiError(e),
  });
}
