import { useNavigate, useParams } from "react-router-dom";
import { Card } from "@shared/ui/Card";
import { Button } from "@shared/ui/Button";
import { useGetProject } from "../application/useGetProject";
import { isProjectAdmin } from "../domain/Project";

type Column = {
  id: string;
  title: string;
  taskCount?: number;
};

type Lane = {
  id: string;
  title: string;
  columns: Column[];
};

// Static lanes. Replace with API data when available.
const DEFAULT_LANES: Lane[] = [
  {
    id: "planning",
    title: "Planning",
    columns: [
      { id: "backlog", title: "Backlog", taskCount: 0 },
      { id: "next", title: "Next Up", taskCount: 0 },
      { id: "design", title: "Design", taskCount: 0 },
    ],
  },
  {
    id: "execution",
    title: "Execution",
    columns: [
      { id: "todo", title: "To Do", taskCount: 0 },
      { id: "doing", title: "In Progress", taskCount: 0 },
      { id: "review", title: "Code Review", taskCount: 0 },
      { id: "qa", title: "QA", taskCount: 0 },
    ],
  },
  {
    id: "delivery",
    title: "Delivery",
    columns: [
      { id: "ready", title: "Ready to Deploy", taskCount: 0 },
      { id: "done", title: "Done", taskCount: 0 },
    ],
  },
];

function ColumnHeader({ title, count }: { title: string; count?: number }) {
  return (
    <div className="flex items-center justify-between gap-2">
      <h3 className="truncate text-sm font-medium">{title}</h3>
      {typeof count === "number" && (
        <span
          aria-label={`${count} tasks`}
          className="rounded-full bg-[color:var(--color-muted)]/40 px-2 py-0.5 text-xs text-[color:var(--color-muted-foreground)]"
        >
          {count}
        </span>
      )}
    </div>
  );
}

function LaneColumn({ column }: { column: Column }) {
  return (
    <Card role="group" aria-label={column.title} className="w-72 shrink-0 rounded-2xl border bg-white/60 p-3">
      <ColumnHeader title={column.title} count={column.taskCount} />
      <div
        className="mt-3 min-h-40 rounded-xl border border-dashed border-[color:var(--color-border)]/70 bg-white/70 p-3"
        aria-label="Drop zone"
      >
        <p className="text-xs text-[color:var(--color-muted-foreground)]">Cards will appear here.</p>
      </div>
    </Card>
  );
}

function BoardLane({ lane }: { lane: Lane }) {
  return (
    <section aria-labelledby={`lane-${lane.id}`} className="scroll-mt-20 snap-start">
      <div className="mb-2 flex items-center justify-between">
        <h2 id={`lane-${lane.id}`} className="text-base font-semibold">
          {lane.title}
        </h2>
      </div>

      <div className="flex gap-3 overflow-x-auto pb-2" role="list" aria-label={`${lane.title} columns`}>
        {lane.columns.map((c) => (
          <div role="listitem" key={c.id}>
            <LaneColumn column={c} />
          </div>
        ))}
      </div>
    </section>
  );
}

export default function ProjectBoardPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data, isLoading, isError } = useGetProject(id);

  if (isLoading) {
    return (
      <div className="w-full max-w-7xl px-6 py-8">
        <p>Loading…</p>
      </div>
    );
  }
  if (isError || !data) {
    return (
      <div className="w-full max-w-7xl px-6 py-8">
        <p className="text-red-600">Failed to load project.</p>
      </div>
    );
  }

  const canManage = isProjectAdmin(data.currentUserRole);
  const lanes = DEFAULT_LANES;

  return (
    <div className="w-full max-w-7xl px-6 py-8 text-[color:var(--color-foreground)]">
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">{data.name}</h1>
          <p className="mt-1 text-sm text-[color:var(--color-muted-foreground)]">
            Drag cards across lanes and columns to reflect progress.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button onClick={() => navigate(`/projects/${data.id}/members`)}>See project members</Button>
          {canManage && (
            <Button onClick={() => navigate(`/projects/${data.id}/settings`)}>Project settings</Button>
          )}
        </div>
      </div>

      <div className="grid gap-6">
        {lanes.map((lane) => (
          <BoardLane key={lane.id} lane={lane} />
        ))}
      </div>

      <div className="mt-8">
        <Card className="rounded-2xl border bg-white/50 p-4">
          <div className="flex flex-wrap items-center gap-3">
            <span className="text-sm text-[color:var(--color-muted-foreground)]">Shortcuts:</span>
            <kbd className="rounded-md border bg-white/70 px-2 py-0.5 text-xs">N</kbd>
            <span className="text-sm">new card</span>
            <kbd className="rounded-md border bg-white/70 px-2 py-0.5 text-xs">/</kbd>
            <span className="text-sm">search</span>
          </div>
        </Card>
      </div>
    </div>
  );
}
