import { useState, useMemo } from "react";
import { Navigate, Link, useNavigate } from "react-router-dom";
import { register } from "@features/auth/application/auth.usecases";
import { useAuthStore } from "@shared/store/auth.store";
import { useApiError } from "@shared/hooks/useApiError";

import { Card } from "@shared/ui/Card";
import { Label } from "@shared/ui/Label";
import { Input } from "@shared/ui/Input";
import { Button } from "@shared/ui/Button";
import { FormErrorText } from "@shared/ui/FormErrorText";
import { Checkbox } from "@shared/ui/Checkbox";

function isValidEmail(value: string): boolean {
  const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return re.test(value);
}

function passwordError(value: string): string | null {
  if (value.length < 8) return "Password must be at least 8 characters.";
  if (!/[A-Z]/.test(value)) return "Password must include at least one uppercase letter.";
  if (!/[^A-Za-z0-9]/.test(value)) return "Password must include at least one special character.";
  return null;
}

export function RegisterPage() {
  const isAuth = useAuthStore((s) => s.isAuthenticated);
  const navigate = useNavigate();

  // Fields
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  // Focus/touched control
  const [touchedEmail, setTouchedEmail] = useState(false);
  const [touchedPassword, setTouchedPassword] = useState(false);
  const [touchedConfirm, setTouchedConfirm] = useState(false);
  const [wasSubmitted, setWasSubmitted] = useState(false);

  // Show/hide password
  const [showPasswords, setShowPasswords] = useState(false);

  // Submission + API error
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<unknown>(null);
  const uiError = useApiError(error);

  // Server-side validation map (only when an API 422 happened)
  const isValidation =
    error !== null &&
    uiError.status === 422 &&
    typeof uiError.details === "object" &&
    uiError.details !== null;

  const fieldErrors = useMemo(() => {
    if (!isValidation) return {} as Record<string, string[]>;
    const raw = uiError.details as Record<string, string[] | string>;
    const norm: Record<string, string[]> = {};
    for (const [k, v] of Object.entries(raw)) {
      norm[k.toLowerCase()] = Array.isArray(v) ? v : [String(v)];
    }
    return norm;
  }, [isValidation, uiError.details]);

  const serverEmailErrors = fieldErrors["email"] ?? [];
  const serverPasswordErrors = fieldErrors["password"] ?? [];

  // Local validations shown only after blur (touched) or on submit
  const showEmailValidation = (touchedEmail || wasSubmitted) && email.length > 0;
  const showPwdValidation = (touchedPassword || wasSubmitted) && password.length > 0;
  const showConfirmValidation = (touchedConfirm || wasSubmitted) && confirmPassword.length > 0;

  const emailLocalError =
    showEmailValidation && !isValidEmail(email) ? "Invalid email format." : null;
  const pwdLocalErr = showPwdValidation ? passwordError(password) : null;
  const confirmPwdLocalErr =
    showConfirmValidation && password !== confirmPassword ? "Passwords do not match." : null;

  // Submit enablement
  const emailOk = isValidEmail(email);
  const pwdOk = passwordError(password) === null;
  const confirmOk = confirmPassword.length > 0 && password === confirmPassword;
  const canSubmit = emailOk && pwdOk && confirmOk;

  if (isAuth) return <Navigate to="/me" replace />;

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setWasSubmitted(true);
    setTouchedEmail(true);
    setTouchedPassword(true);
    setTouchedConfirm(true);
    setError(null);

    if (!canSubmit) return;

    setSubmitting(true);
    try {
      await register({ email, password });
      navigate("/me", { replace: true });
    } catch (err) {
      setError(err);
    } finally {
      setSubmitting(false);
    }
  }

  const hasApiError = error !== null && Boolean(uiError.title);
  const alertId = hasApiError ? "register-alert" : undefined;

  // Compose field messages
  const emailAllErrors: string[] = [
    ...(emailLocalError ? [emailLocalError] : []),
    ...serverEmailErrors,
  ];
  const passwordAllErrors: string[] = [
    ...(pwdLocalErr ? [pwdLocalErr] : []),
    ...serverPasswordErrors,
  ];
  const confirmAllErrors: string[] = confirmPwdLocalErr ? [confirmPwdLocalErr] : [];

  return (
    <main className="mx-auto w-full max-w-sm p-6">
      <Card title="Create account" className="w-full">
        {hasApiError && (
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
              onBlur={() => setTouchedEmail(true)}
              autoComplete="email"
              className="mt-1"
              invalid={emailAllErrors.length > 0}
              errorId={emailAllErrors.length ? "email-error" : undefined}
            />
            {emailAllErrors.length ? (
              <FormErrorText id="email-error">{emailAllErrors.join(" ")}</FormErrorText>
            ) : null}
          </div>

          <div>
            <Label htmlFor="password" size="md" requiredMark>Password</Label>
            <Input
              id="password"
              type={showPasswords ? "text" : "password"}
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              onBlur={() => setTouchedPassword(true)}
              autoComplete="new-password"
              className="mt-1"
              invalid={passwordAllErrors.length > 0}
              errorId={passwordAllErrors.length ? "password-error" : undefined}
            />
            {passwordAllErrors.length ? (
              <FormErrorText id="password-error">{passwordAllErrors.join(" ")}</FormErrorText>
            ) : null}
          </div>

          <div>
            <Label htmlFor="confirmPassword" size="md" requiredMark>
              Confirm Password
            </Label>
            <Input
              id="confirmPassword"
              type={showPasswords ? "text" : "password"}
              required
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              onBlur={() => setTouchedConfirm(true)}
              autoComplete="new-password"
              className="mt-1"
              invalid={confirmAllErrors.length > 0}
              errorId={confirmAllErrors.length ? "confirm-password-error" : undefined}
            />
            {confirmAllErrors.length ? (
              <FormErrorText id="confirm-password-error">
                {confirmAllErrors.join(" ")}
              </FormErrorText>
            ) : null}
          </div>

          <div className="flex items-center gap-2">
            <Checkbox
              id="showPasswords"
              checked={showPasswords}
              onChange={(e) => setShowPasswords(e.currentTarget.checked)}
            />
            <Label htmlFor="showPasswords" size="sm">
              Show passwords
            </Label>
          </div>

          <Button
            type="submit"
            className="w-full"
            isLoading={submitting}
            disabled={!canSubmit || submitting}
            aria-disabled={!canSubmit || submitting}
          >
            {submitting ? <span className="spinner" aria-hidden="true" /> : null}
            {submitting ? "Creating…" : "Create account"}
          </Button>
        </form>

        <p className="mt-4 text-sm">
          Already have an account? <Link to="/login" className="underline">Sign in</Link>
        </p>
      </Card>
    </main>
  );
}
