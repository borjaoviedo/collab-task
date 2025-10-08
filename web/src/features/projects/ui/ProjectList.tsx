import type { Project } from "../domain/Project";
import { ProjectCard } from "./ProjectCard";

type Props = { projects: Project[] };

export function ProjectList({ projects }: Props) {
  if (projects.length === 0) {
    return (
      <div className="rounded-2xl border p-8 bg-white/60 text-center">
        <p className="text-sm text-[color:var(--color-muted-foreground)]">No projects yet.</p>
      </div>
    );
  }

  return (
    <ul className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      {projects.map((p) => (
        <li key={p.id}>
          <ProjectCard project={p} />
        </li>
      ))}
    </ul>
  );
}
