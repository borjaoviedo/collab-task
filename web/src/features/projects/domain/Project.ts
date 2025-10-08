export type ProjectId = string;

export interface Project {
  id: ProjectId;
  name: string;
  slug: string;
  membersCount: number;
  currentUserRole: string;
}

export type ProjectRole = "Owner" | "Admin" | "Member" | "Reader";

export interface ProjectDetail extends Project {
  currentUserRole: ProjectRole;
}

export type CreateProjectRequest = { name: string };
export type CreateProjectResponse = { id: ProjectId };

export function isProjectAdmin(role: ProjectRole): boolean {
  return role === "Admin" || role === "Owner";
}