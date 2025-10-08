import { useState } from "react"
import { useCreateProject } from "../application/useCreateProject"
import { Card } from "@shared/ui/Card"
import { Label } from "@shared/ui/Label"
import { Input } from "@shared/ui/Input"
import { Button } from "@shared/ui/Button"
import { FormErrorText } from "@shared/ui/FormErrorText"
import clsx from "clsx"

type Props = { onCreated: (projectId: string) => void; className?: string }

export function CreateProjectForm({ onCreated, className }: Props) {
  const [name, setName] = useState("")
  const [touched, setTouched] = useState(false)
  const { mutateAsync, isPending, isError, error } = useCreateProject()
  const isValid = name.trim().length >= 3

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault()
    setTouched(true)
    if (!isValid) return
    const created = await mutateAsync({ name: name.trim() }) // returns Project
    onCreated(created) // <-- id string
  }

  return (
    <Card className={clsx("p-4 bg-white/70", className)}>
      <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-3 sm:flex-row sm:items-end">
        <div className="flex-1 min-w-56">
          <Label htmlFor="projectName">Project name</Label>
          <Input
            id="projectName"
            value={name}
            onChange={(e) => setName(e.target.value)}
            onBlur={() => setTouched(true)}
            placeholder="e.g., Backend Revamp"
            disabled={isPending}
            aria-invalid={touched && !isValid}
            aria-describedby="projectNameHelp projectNameError"
          />
          <p id="projectNameHelp" className="text-xs text-[color:var(--color-muted-foreground)] mt-1">
            Minimum 3 characters.
          </p>
          {touched && !isValid && (
            <FormErrorText id="projectNameError">Please enter at least 3 characters.</FormErrorText>
          )}
        </div>

        <Button type="submit" disabled={!isValid || isPending} aria-live="polite">
          {isPending ? "Creating…" : "Create project"}
        </Button>

        {isError && <FormErrorText>{(error as { message?: string }).message ?? "Creation failed."}</FormErrorText>}
      </form>
    </Card>
  )
}
