import { Link } from "react-router-dom";
import type { Project } from "../domain/Project";
import { Card } from "@shared/ui/Card";

type Props = { project: Project };

export function ProjectCard({ project }: Props) {
  return (
    <Card className="p-4 hover:shadow-md transition-shadow bg-white/70">
      <Link to={`/projects/${project.id}`} className="block">
        <h2 className="text-lg font-semibold">{project.name}</h2>
        <p className="text-xs text-[color:var(--color-muted-foreground)] mt-1">Open project board</p>
      </Link>
    </Card>
  );
}
