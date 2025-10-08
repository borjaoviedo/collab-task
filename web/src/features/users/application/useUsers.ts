import { useQuery } from "@tanstack/react-query";
import { getUsers } from "../infrastructure/users.api";
import type { User } from "../domain/User";

type Options = { enabled?: boolean };

export function useUsers({ enabled = true }: Options = {}) {
  return useQuery<User[], Error>({
    queryKey: ["users", "all"],
    queryFn: () => getUsers(),
    enabled,
    staleTime: 30_000,
  });
}