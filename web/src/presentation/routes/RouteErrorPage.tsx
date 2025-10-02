import { isRouteErrorResponse, useRouteError, Link } from "react-router-dom";
import { Card } from "@shared/ui/Card";
import { Button } from "@shared/ui/Button";

export function RouteErrorPage() {
  const err = useRouteError();
  const status = isRouteErrorResponse(err) ? err.status : 500;

  const title = status === 404 ? "Page not found" : "Something went wrong";
  const message =
    status === 404
      ? "The URL you tried to access does not exist."
      : "An error occurred while loading this route.";

  return (
    <main className="mx-auto w-full max-w-md p-10">
      <Card title={title} className="text-center">
        <p className="text-sm text-[color:var(--color-muted-foreground)]">{message}</p>
        <div className="mt-6 flex justify-center">
          <Button asChild variant="primary">
            <Link to="/">Go home</Link>
          </Button>
        </div>
      </Card>
    </main>
  );
}
