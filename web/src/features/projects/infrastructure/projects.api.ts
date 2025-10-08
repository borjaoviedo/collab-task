import { apiFetchJsonAuth } from "@shared/api/authClient";
import type { 
  Project, 
  ProjectDetail, 
  CreateProjectRequest,
  CreateProjectResponse,
 } from "../domain/Project";

export async function getProjects(): Promise<Project[]> {
  return apiFetchJsonAuth<Project[]>("/projects", { method: "GET" });
}

export async function getProjectById(id: string): Promise<ProjectDetail> {
  return apiFetchJsonAuth<ProjectDetail>(`/projects/${id}`, { method: "GET" });
}

export async function createProject(payload: CreateProjectRequest): Promise<CreateProjectResponse> {
  return apiFetchJsonAuth<CreateProjectResponse>("/projects", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
}
