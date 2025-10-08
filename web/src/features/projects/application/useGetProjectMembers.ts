import { useCallback, useEffect, useState } from "react";
import type { ProjectMember } from "../domain/ProjectMember";
import { getProjectMembers } from "../infrastructure/projectMembers.api";

type State =
  | { status: "idle"; data: null; error: null }
  | { status: "loading"; data: null; error: null }
  | { status: "success"; data: ProjectMember[]; error: null }
  | { status: "error"; data: null; error: Error };

export function useGetProjectMembers(projectId: string | undefined) {
  const [state, setState] = useState<State>({ status: "idle", data: null, error: null });
  const [tick, setTick] = useState(0);

  const refetch = useCallback(() => setTick((n) => n + 1), []);

  useEffect(() => {
    if (!projectId) return;
    let active = true;
    setState({ status: "loading", data: null, error: null });

    getProjectMembers(projectId)
      .then((m) => { if (active) setState({ status: "success", data: m, error: null }); })
      .catch((e: unknown) => {
        if (!active) return;
        const err = e instanceof Error ? e : new Error("Failed to load members");
        setState({ status: "error", data: null, error: err });
      });

    return () => { active = false; };
  }, [projectId, tick]);

  return {
    data: state.status === "success" ? state.data : null,
    isLoading: state.status === "loading",
    isError: state.status === "error",
    error: state.status === "error" ? state.error : null,
    refetch,
  };
}
