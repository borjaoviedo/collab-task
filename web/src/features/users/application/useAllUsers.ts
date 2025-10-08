import { useUsers } from "./useUsers";
export type UserOption = { id: string; name: string | null; email: string | null; };
type UseAllUsersResult = { users: UserOption[]; isLoading: boolean; isError: boolean; };

export function useAllUsers(): UseAllUsersResult {
  const { data, isLoading, isError } = useUsers();
  return {
    users: (data ?? []).map(u => ({ id: u.id, name: u.name ?? null, email: u.email ?? null })),
    isLoading,
    isError,
  };
}