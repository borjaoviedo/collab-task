import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Card } from "@shared/ui/Card";
import { Label } from "@shared/ui/Label";
import { Input } from "@shared/ui/Input";
import { Button } from "@shared/ui/Button";
import { FormErrorText } from "@shared/ui/FormErrorText";
import { useGetProject } from "@features/projects/application/useGetProject";
import { useRenameProject } from "../application/useRenameProject";
import { useDeleteProject } from "../application/useDeleteProject";
import { isProjectAdmin, isProjectOwner } from "@features/projects/domain/Project";

export default function ProjectSettingsPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data, isLoading, isError, error } = useGetProject(id);

  const [name, setName] = useState("");
  const [confirm, setConfirm] = useState("");
  const [rowVersion, setRowVersion] = useState<string>("");
  const [feedback, setFeedback] = useState<string | null>(null);

  const { mutateAsync: rename, isPending: renaming, error: renameError } = useRenameProject(id!);
  const { mutateAsync: del, isPending: deleting, error: deleteError } = useDeleteProject(id!);

  useEffect(() => {
    if (data) {
      setName(data.name);
      setRowVersion(data.rowVersion);
    }
  }, [data]);

  if (isLoading) return <div className="w-full max-w-3xl px-6 py-8"><p>Loading…</p></div>;
  if (isError || !data) {
    return (
      <div className="w-full max-w-3xl px-6 py-8">
        <FormErrorText>Failed to load project: {String(error)}</FormErrorText>
      </div>
    );
  }

  const canAdmin = isProjectAdmin(data.currentUserRole);
  const canOwner = isProjectOwner(data.currentUserRole);

  async function onSave() {
    if (!canAdmin || !rowVersion) return;
    await rename({ name, rowVersion });
    setFeedback("Project name updated");
    window.setTimeout(() => setFeedback(null), 3000);
  }

  async function onDelete() {
    if (!canOwner || !rowVersion) return;
    await del({ rowVersion });
    navigate("/projects");
  }

  return (
    <div className="w-full max-w-3xl px-6 py-8 space-y-6">
      <h1 className="text-2xl font-semibold">Project settings</h1>

      {/* Rename */}
      <Card className="p-6 space-y-4">
        <div className="space-y-2">
          <Label htmlFor="name">Rename project</Label>
          <Input
            id="name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            disabled={!canAdmin || renaming}
          />
        </div>

        {/* Buttons separated from input */}
        <div className="mt-2 flex gap-2">
          <Button
            onClick={onSave}
            disabled={!canAdmin || renaming || name.trim().length === 0}
            aria-busy={renaming}
          >
            Save name
          </Button>
        </div>

        {/* Success + errors */}
        {feedback && (
          <p role="status" aria-live="polite" className="text-sm text-emerald-700">
            {feedback}
          </p>
        )}
        {renameError && <FormErrorText>{String(renameError)}</FormErrorText>}
        {!canAdmin && (
          <p className="text-sm text-slate-500">
            You need admin or owner role to rename this project.
          </p>
        )}
      </Card>

      {/* Danger zone: Delete */}
      <Card className="p-6 space-y-4 border-rose-200">
        <h2 className="text-lg font-semibold text-rose-700">Danger zone</h2>
        <p className="text-sm text-slate-600">
          Deleting a project is permanent. Type the project name to confirm.
        </p>
        <Input
          placeholder="Type project name to confirm"
          value={confirm}
          onChange={(e) => setConfirm(e.target.value)}
          disabled={!canOwner || deleting}
        />

        {/* Buttons separated from input */}
        <div className="mt-2">
          <Button
            onClick={onDelete}
            disabled={!canOwner || deleting || confirm !== data.name}
            aria-busy={deleting}
          >
            Delete project
          </Button>
        </div>

        {deleteError && <FormErrorText>{String(deleteError)}</FormErrorText>}
        {!canOwner && (
          <p className="text-sm text-slate-500">Only the owner can delete this project.</p>
        )}
      </Card>
    </div>
  );
}
