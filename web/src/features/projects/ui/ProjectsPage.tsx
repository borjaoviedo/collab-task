import { useNavigate } from "react-router-dom"
import { CreateProjectForm } from "./CreateProjectForm"
import { ProjectList } from "./ProjectList"
import { useProjects } from "@features/projects/application/useProjects"

export default function ProjectsPage() {
  const navigate = useNavigate()
  const { data, isLoading, isError } = useProjects()

  return (
    <div className="w-full max-w-5xl px-6 py-8 text-[color:var(--color-foreground)]">
      <header className="mb-6">
        <h1 className="text-2xl font-semibold">Projects</h1>
        <p className="text-sm text-[color:var(--color-muted-foreground)]">
          Your projects and a quick way to create a new one.
        </p>
      </header>

      <CreateProjectForm onCreated={(id) => navigate(`/projects/${id}`)} className="mb-8" />

      {isLoading && <p className="text-sm text-[color:var(--color-muted-foreground)]">Loading…</p>}
      {isError && <p className="text-sm text-red-600">Failed to load projects.</p>}
      {data && <ProjectList projects={data} />}
    </div>
  )
}
