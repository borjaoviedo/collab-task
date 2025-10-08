import { useNavigate, useParams } from "react-router-dom";
import { Card } from "@shared/ui/Card";
import { Button } from "@shared/ui/Button";
import { useGetProject } from "../application/useGetProject";
import { isProjectAdmin } from "../domain/Project";

export default function ProjectBoardPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data, isLoading, isError } = useGetProject(id);

  if (isLoading) {
    return <div className="w-full max-w-6xl px-6 py-8"><p>Loading…</p></div>;
  }
  if (isError || !data) {
    return <div className="w-full max-w-6xl px-6 py-8"><p className="text-red-600">Failed to load project.</p></div>;
  }

  const canManageUsers = isProjectAdmin(data.currentUserRole);

  return (
    <div className="w-full max-w-6xl px-6 py-8 text-[color:var(--color-foreground)]">
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">{data.name}</h1>
        </div>
        <div className="flex items-center gap-2">
          {canManageUsers && (
            <Button onClick={() => navigate(`/projects/${data.id}/members`)}>
              Manage users
            </Button>
          )}
        </div>
      </div>

      <Card className="rounded-2xl border p-8 bg-white/60">
        <p className="text-sm text-[color:var(--color-muted-foreground)]">
          Board shell is empty for now. Columns and tasks will appear here.
        </p>
      </Card>
    </div>
  );
}
