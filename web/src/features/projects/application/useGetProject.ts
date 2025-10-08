import { useEffect, useState } from "react";
import type { ProjectDetail } from "../domain/Project";
import { getProjectById } from "../infrastructure/projects.api";

type State =
  | { status: "idle"; data: null; error: null }
  | { status: "loading"; data: null; error: null }
  | { status: "success"; data: ProjectDetail; error: null }
  | { status: "error"; data: null; error: Error };

export function useGetProject(id: string | undefined) {
  const [state, setState] = useState<State>({ status: "idle", data: null, error: null });

  useEffect(() => {
    if (!id) return;
    let active = true;
    setState({ status: "loading", data: null, error: null });

    getProjectById(id)
      .then((p) => { if (active) setState({ status: "success", data: p, error: null }); })
      .catch((e: unknown) => {
        if (!active) return;
        const err = e instanceof Error ? e : new Error("Failed to load project");
        setState({ status: "error", data: null, error: err });
      });

    return () => { active = false; };
  }, [id]);

  return {
    data: state.status === "success" ? state.data : null,
    isLoading: state.status === "loading",
    isError: state.status === "error",
    error: state.status === "error" ? state.error : null,
  };
}
