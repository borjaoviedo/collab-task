import { useEffect, useState } from "react";
import type { Project } from "../domain/Project";
import { getProjects } from "../infrastructure/projects.api";

type State =
  | { status: "idle"; data: null; error: null }
  | { status: "loading"; data: null; error: null }
  | { status: "success"; data: Project[]; error: null }
  | { status: "error"; data: null; error: Error };

export function useGetProjects() {
  const [state, setState] = useState<State>({
    status: "idle",
    data: null,
    error: null,
  });

  useEffect(() => {
    let active = true;
    setState({ status: "loading", data: null, error: null });

    getProjects()
      .then((projects) => {
        if (!active) return;
        setState({ status: "success", data: projects, error: null });
      })
      .catch((e: unknown) => {
        if (!active) return;
        const err = e instanceof Error ? e : new Error("Failed to load projects");
        setState({ status: "error", data: null, error: err });
      });

    return () => {
      active = false;
    };
  }, []);

  return {
    data: state.status === "success" ? state.data : null,
    isLoading: state.status === "loading",
    isError: state.status === "error",
    error: state.status === "error" ? state.error : null,
  };
}
