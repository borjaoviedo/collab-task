import { useQuery } from "@tanstack/react-query";
import { getUsers } from "../infrastructure/users.api";
import type { User } from "../domain/User";

export function useUsers() {
  return useQuery<User[], Error>({
    queryKey: ["users", "all"],
    queryFn: () => getUsers(),
    staleTime: 30_000,
  });
}