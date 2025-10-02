import { useState, useMemo } from "react";
import { Navigate, Link, useNavigate } from "react-router-dom";
import { login } from "@features/auth/application/auth.usecases";
import { useAuthStore } from "@shared/store/auth.store";
import { useApiError } from "@shared/hooks/useApiError";

import { Card } from "@shared/ui/Card";
import { Label } from "@shared/ui/Label";
import { Input } from "@shared/ui/Input";
import { Button } from "@shared/ui/Button";
import { FormErrorText } from "@shared/ui/FormErrorText";

export function LoginPage() {
  const isAuth = useAuthStore((s) => s.isAuthenticated);
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<unknown>(null);
  const uiError = useApiError(error);

  const isValidation = uiError.status === 422 && typeof uiError.details === "object" && uiError.details !== null;
  const fieldErrors = useMemo(() => {
    if (!isValidation) return {} as Record<string, string[]>;
    const raw = uiError.details as Record<string, string[] | string>;
    const norm: Record<string, string[]> = {};
    for (const [k, v] of Object.entries(raw)) {
      norm[k.toLowerCase()] = Array.isArray(v) ? v : [String(v)];
    }
    return norm;
  }, [isValidation, uiError.details]);

  const emailErrors = fieldErrors["email"] ?? [];
  const passwordErrors = fieldErrors["password"] ?? [];

  if (isAuth) return <Navigate to="/me" replace />;

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await login({ email, password });
      navigate("/me", { replace: true });
    } catch (err) {
      setError(err);
    } finally {
      setSubmitting(false);
    }
  }

  const alertId = uiError.title ? "login-alert" : undefined;

  return (
    <main className="mx-auto w-full max-w-sm p-6">
      <Card title="Sign in" className="w-full">
        {uiError.title && (
          <div id={alertId} role="alert" className="mb-4">
            <p className="font-medium">{uiError.title}</p>
            {uiError.message && <p className="text-sm mt-1">{uiError.message}</p>}
          </div>
        )}

        <form onSubmit={onSubmit} className="space-y-4" noValidate aria-describedby={alertId}>
          <div>
            <Label htmlFor="email" size="md" requiredMark>Email</Label>
            <Input
              id="email"
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              autoComplete="email"
              className="mt-1"
              invalid={emailErrors.length > 0}
              errorId={emailErrors.length ? "email-error" : undefined}
            />
            {emailErrors.length ? (
              <FormErrorText id="email-error">
                {emailErrors.join(" ")}
              </FormErrorText>
            ) : null}
          </div>

          <div>
            <Label htmlFor="password" size="md" requiredMark>Password</Label>
            <Input
              id="password"
              type="password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              autoComplete="current-password"
              className="mt-1"
              invalid={passwordErrors.length > 0}
              errorId={passwordErrors.length ? "password-error" : undefined}
            />
            {passwordErrors.length ? (
              <FormErrorText id="password-error">
                {passwordErrors.join(" ")}
              </FormErrorText>
            ) : null}
          </div>

          <Button type="submit" className="w-full" isLoading={submitting} disabled={submitting}>
            {submitting ? (<span className="spinner" aria-hidden="true" />) : null}
            {submitting ? "Signing in…" : "Sign in"}
          </Button>
        </form>

        <p className="mt-4 text-sm">
          No account? <Link to="/register" className="underline">Register</Link>
        </p>
      </Card>
    </main>
  );
}
