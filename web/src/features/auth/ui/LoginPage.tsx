import { useMemo, useRef, useState } from "react";
import { Navigate, Link, useNavigate } from "react-router-dom";
import { login } from "@features/auth/application/auth.usecases";
import { useAuthStore } from "@shared/store/auth.store";
import { useApiError } from "@shared/hooks/useApiError";
import { Card } from "@shared/ui/Card";
import { Label } from "@shared/ui/Label";
import { Input } from "@shared/ui/Input";
import { Button } from "@shared/ui/Button";
import { FormErrorText } from "@shared/ui/FormErrorText";
import { Checkbox } from "@shared/ui/Checkbox";
import {
  isValidEmail,
  passwordError,
  normalizeServerFieldErrors,
} from "@shared/validation/auth";
import { ApiError } from "@shared/api/client";

export function LoginPage() {
  const isAuth = useAuthStore((s) => s.isAuthenticated);
  const navigate = useNavigate();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);

  const [wasSubmitted, setWasSubmitted] = useState(false);
  const [pwdFocused, setPwdFocused] = useState(false);
  const [touchedEmail, setTouchedEmail] = useState(false);
  const [touchedPwd, setTouchedPwd] = useState(false);

  const emailRef = useRef<HTMLInputElement | null>(null);
  const pwdRef = useRef<HTMLInputElement | null>(null);

  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<unknown>(null);
  const uiError = useApiError(error);

  // Local flag to convert 401 on /auth/login into field-level error, not a global alert
  const [invalidCreds, setInvalidCreds] = useState(false);

  const fieldErrors = useMemo(() => normalizeServerFieldErrors(uiError), [uiError]);

  const showEmailVal = (wasSubmitted || touchedEmail) && email.length > 0;
  const showPwdVal = (wasSubmitted || touchedPwd || pwdFocused) && password.length > 0;

  const emailLocalErr = showEmailVal && !isValidEmail(email) ? "Invalid email format" : null;
  const pwdLocalErr = showPwdVal ? passwordError(password) : null;

  const serverEmailErrs = fieldErrors["email"] ?? [];
  let serverPwdErrs = fieldErrors["password"] ?? [];

  // Inject field-level error for invalid credentials
  if (invalidCreds) {
    serverPwdErrs = [...serverPwdErrs, "Email or password is incorrect."];
  }

  const emailAllErrs = [...(emailLocalErr ? [emailLocalErr] : []), ...serverEmailErrs];
  const pwdAllErrs = [...(pwdLocalErr ? [pwdLocalErr] : []), ...serverPwdErrs];

  const emailOk = isValidEmail(email);
  const pwdOk = passwordError(password) === null;
  const canSubmit = emailOk && pwdOk;

  if (isAuth) return <Navigate to="/me" replace />;

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setWasSubmitted(true);
    setTouchedEmail(true);
    setTouchedPwd(true);
    setInvalidCreds(false);
    setError(null);

    if (!canSubmit) {
      if (!emailOk) emailRef.current?.focus();
      else if (!pwdOk) pwdRef.current?.focus();
      return;
    }

    setSubmitting(true);
    try {
      await login({ email, password });
      navigate("/me", { replace: true });
    } catch (err) {
      // Convert 401 from /auth/login into field-level error, not global alert
      if (err instanceof ApiError && err.status === 401) {
        setInvalidCreds(true);
        // Avoid showing the global alert area for this case
        setError(null);
        // Focus on password field after paint
        setTimeout(() => pwdRef.current?.focus(), 0);
      } else {
        setError(err);
        setTimeout(() => {
          if ((fieldErrors["email"]?.length ?? 0) > 0) emailRef.current?.focus();
          else if ((fieldErrors["password"]?.length ?? 0) > 0) pwdRef.current?.focus();
        }, 0);
      }
    } finally {
      setSubmitting(false);
    }
  }

  // Show alert only when there is an API error that is NOT invalid credentials on login
  const hasApiError = uiError != null && !invalidCreds;
  const alertId = hasApiError ? "login-alert" : undefined;

  return (
    <main className="mx-auto w-full max-w-sm p-6">
      <Card title="Sign in" className="w-full">
        {hasApiError && (
          <div id={alertId} role="alert" aria-live="assertive" className="mb-4">
            {uiError?.title && <p className="font-medium">{uiError.title}</p>}
            {uiError?.message && <p className="text-sm mt-1">{uiError.message}</p>}
          </div>
        )}

        <form onSubmit={onSubmit} className="space-y-4" noValidate aria-describedby={alertId}>
          <div>
            <Label htmlFor="email" size="md" requiredMark>Email</Label>
            <Input
              ref={emailRef}
              id="email"
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              onBlur={() => setTouchedEmail(true)}
              autoComplete="email"
              className="mt-1"
              invalid={emailAllErrs.length > 0}
              errorId={emailAllErrs.length ? "email-error" : undefined}
            />
            {emailAllErrs.length ? (
              <FormErrorText id="email-error" aria-live="polite">
                {emailAllErrs.join(" ")}
              </FormErrorText>
            ) : null}
          </div>

          <div>
            <Label htmlFor="password" size="md" requiredMark>Password</Label>
            <Input
              ref={pwdRef}
              id="password"
              type={showPassword ? "text" : "password"}
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              onFocus={() => setPwdFocused(true)}
              onBlur={() => {
                setPwdFocused(false);
                setTouchedPwd(true);
              }}
              autoComplete="current-password"
              className="mt-1"
              invalid={pwdAllErrs.length > 0}
              errorId={pwdAllErrs.length ? "password-error" : undefined}
            />
            {pwdAllErrs.length ? (
              <FormErrorText id="password-error" aria-live="polite">
                {pwdAllErrs.join(" ")}
              </FormErrorText>
            ) : null}
          </div>

          <div className="flex items-center gap-2">
            <Checkbox
              id="showPassword"
              checked={showPassword}
              onChange={(e) => setShowPassword(e.currentTarget.checked)}
            />
            <Label htmlFor="showPassword" size="sm">Show password</Label>
          </div>

          <Button
            type="submit"
            className="w-full"
            isLoading={submitting}
            disabled={!canSubmit || submitting}
            aria-disabled={!canSubmit || submitting}
          >
            {submitting ? <span className="spinner" aria-hidden="true" /> : null}
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
