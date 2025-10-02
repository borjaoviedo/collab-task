import { Link } from "react-router-dom";
import { Button } from "@shared/ui/Button";
import { Card } from "@shared/ui/Card";
import { useAuthStore } from "@shared/store/auth.store";

export function HomePage() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const logout = useAuthStore((s) => s.logout);

  return (
    <section className="w-full max-w-5xl mx-auto grid gap-8 py-10 px-4">
      {!isAuthenticated ? <PublicHero /> : (
        <>
          <header className="grid gap-2 text-center md:text-left">
            <h1 className="text-3xl font-semibold tracking-tight">CollabTask</h1>
            <p className="text-[color:var(--color-muted-foreground)]">
              Your collaborative hub for focused task management.
            </p>
          </header>

          <div className="grid gap-6 md:grid-cols-3">
            {/* Card 1: Board Collaboration */}
            <Card className="p-6 md:col-span-2">
              <h2 className="text-xl font-medium">Work together on shared boards</h2>
              <p className="text-sm text-[color:var(--color-muted-foreground)] mt-1">
                See teammates’ updates as they happen, assign tasks in context, and track progress on the same board without refreshing.
              </p>

              <div className="mt-6 grid gap-3 sm:flex">
                <Button asChild aria-label="Open projects">
                  <Link to="/projects">Open projects</Link>
                </Button>
                {typeof logout === "function" && (
                  <Button asChild onClick={logout} aria-label="Sign out">
                    Sign out
                  </Button>
                )}
              </div>
            </Card>

            {/* Card 2: Quick links */}
            <Card className="p-6">
              <h3 className="font-medium">Quick links</h3>
              <ul className="mt-3 grid gap-2 text-sm">
                <li>
                  <Link className="underline underline-offset-4" to="/todos?filter=today">
                    Today’s tasks
                  </Link>
                </li>
                <li>
                  <Link className="underline underline-offset-4" to="/todos?filter=overdue">
                    Overdue
                  </Link>
                </li>
                <li>
                  <Link className="underline underline-offset-4" to="/settings">
                    Settings
                  </Link>
                </li>
              </ul>
            </Card>
          </div>
        </>
      )}
    </section>
  );
}

function PublicHero() {
  return (
    <div className="text-center grid gap-8 place-items-center">
      <h1 className="text-4xl sm:text-5xl font-semibold tracking-tight">
        Collaborate. Prioritize. Ship.
      </h1>
      <p className="max-w-prose text-[color:var(--color-muted-foreground)]">
        CollabTask keeps teams aligned and work moving. Sign in to continue
        or create a new account to start collaborating.
      </p>
      <div className="grid gap-4 sm:flex">
        <Button asChild className="flex-1" aria-label="Go to sign in">
          <Link to="/login">Sign in</Link>
        </Button>
        <Button asChild className="flex-1" aria-label="Go to registration">
          <Link to="/register">Create account</Link>
        </Button>
      </div>

      <div className="grid gap-3 sm:grid-cols-3 w-full max-w-4xl mt-6">
        <Card >
          <h3 className="font-medium">Frictionless</h3>
          <p className="text-sm text-[color:var(--color-muted-foreground)] mt-1">Clear and accessible UI.</p>
        </Card>
        <Card >
          <h3 className="font-medium">Focus first</h3>
          <p className="text-sm text-[color:var(--color-muted-foreground)] mt-1">Only what matters on screen.</p>
        </Card>
        <Card >
          <h3 className="font-medium">Shared context</h3>
          <p className="text-sm text-[color:var(--color-muted-foreground)] mt-1">Everyone sees the same source of truth.</p>
        </Card>
      </div>
    </div>
  );
}
