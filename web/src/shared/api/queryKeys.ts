export const qk = {
  projects: () => ['projects'] as const,
  project: (id: string) => ['project', id] as const,
  members: (projectId: string) => ['project', projectId, 'members'] as const,
  board: (projectId: string) => ['project', projectId, 'board'] as const,
}