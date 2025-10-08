import { useEffect, useState } from "react";
import { getUserById } from "../infrastructure/users.api";
import type { User } from "../domain/User";

type State =
  | { status: "idle"; data: null; error: null }
  | { status: "loading"; data: null; error: null }
  | { status: "success"; data: User; error: null }
  | { status: "error"; data: null; error: Error };

export function useUserDetail(id?: string) {
  const [state, setState] = useState<State>({ status: "idle", data: null, error: null });

  useEffect(() => {
    if (!id) return;
    let active = true;
    setState({ status: "loading", data: null, error: null });
    getUserById(id)
      .then((u) => { if (active) setState({ status: "success", data: u, error: null }); })
      .catch((e: unknown) => {
        if (!active) return;
        const err = e instanceof Error ? e : new Error("Failed to load user");
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
