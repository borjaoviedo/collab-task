import { useMemo, useRef, useState } from "react";
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
import {
  isValidEmail,
  passwordError,
  normalizeServerFieldErrors,
} from "@shared/validation/auth";

export function RegisterPage() {
  const isAuth = useAuthStore((s) => s.isAuthenticated);
  const navigate = useNavigate();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPasswords, setShowPasswords] = useState(false);

  const [wasSubmitted, setWasSubmitted] = useState(false);
  const [pwdFocused, setPwdFocused] = useState(false);
  const [confirmFocused, setConfirmFocused] = useState(false);
  const [touchedEmail, setTouchedEmail] = useState(false);
  const [touchedPwd, setTouchedPwd] = useState(false);
  const [touchedConfirm, setTouchedConfirm] = useState(false);

  const emailRef = useRef<HTMLInputElement | null>(null);
  const pwdRef = useRef<HTMLInputElement | null>(null);
  const confirmRef = useRef<HTMLInputElement | null>(null);

  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<unknown>(null);
  const uiError = useApiError(error);

  const fieldErrors = useMemo(() => normalizeServerFieldErrors(uiError), [uiError]);

  const pwdOk = passwordError(password) === null;

  const showEmailVal = (wasSubmitted || touchedEmail) && email.length > 0;
  const showPwdVal = (wasSubmitted || touchedPwd || pwdFocused) && password.length > 0;
  const showConfirmVal =
    (wasSubmitted || touchedConfirm || confirmFocused) && confirmPassword.length > 0;

  const emailLocalErr =
    showEmailVal && !isValidEmail(email) ? "Invalid email format" : null;
  const pwdLocalErr = showPwdVal ? passwordError(password) : null;
  const confirmLocalErr =
    showConfirmVal && password !== confirmPassword ? "Passwords do not match" : null;

  const serverEmailErrs = fieldErrors["email"] ?? [];
  const serverPwdErrs = fieldErrors["password"] ?? [];

  const emailAllErrs = [...(emailLocalErr ? [emailLocalErr] : []), ...serverEmailErrs];
  const pwdAllErrs = [...(pwdLocalErr ? [pwdLocalErr] : []), ...serverPwdErrs];
  const confirmAllErrs = [...(confirmLocalErr ? [confirmLocalErr] : [])];

  const emailOk = isValidEmail(email);
  const confirmOk = confirmPassword.length > 0 && password === confirmPassword;
  const canSubmit = emailOk && pwdOk && confirmOk;

  if (isAuth) return <Navigate to="/me" replace />;

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setWasSubmitted(true);
    setTouchedEmail(true);
    setTouchedPwd(true);
    setTouchedConfirm(true);
    setError(null);

    if (!canSubmit) {
      if (!emailOk) emailRef.current?.focus();
      else if (!pwdOk) pwdRef.current?.focus();
      else if (!confirmOk) confirmRef.current?.focus();
      return;
    }

    setSubmitting(true);
    try {
      await register({ email, password });
      navigate("/me", { replace: true });
    } catch (err) {
      setError(err);
      setTimeout(() => {
        if ((fieldErrors["email"]?.length ?? 0) > 0) emailRef.current?.focus();
        else if ((fieldErrors["password"]?.length ?? 0) > 0) pwdRef.current?.focus();
      }, 0);
    } finally {
      setSubmitting(false);
    }
  }

  const hasApiError = error !== null && Boolean(uiError.title);
  const alertId = hasApiError ? "register-alert" : undefined;

  return (
    <main className="mx-auto w-full max-w-sm p-6">
      <Card title="Create account" className="w-full">
        {hasApiError && (
          <div id={alertId} role="alert" aria-live="polite" className="mb-4">
            <p className="font-medium">{uiError.title}</p>
            {uiError.message && <p className="text-sm mt-1">{uiError.message}</p>}
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
              type={showPasswords ? "text" : "password"}
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              onFocus={() => setPwdFocused(true)}
              onBlur={() => {
                setPwdFocused(false);
                setTouchedPwd(true);
              }}
              autoComplete="new-password"
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

          <div aria-disabled={!pwdOk}>
            <Label htmlFor="confirmPassword" size="md" requiredMark>
              Confirm Password
            </Label>
            <Input
              ref={confirmRef}
              id="confirmPassword"
              type={showPasswords ? "text" : "password"}
              required
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              onFocus={() => setConfirmFocused(true)}
              onBlur={() => {
                setConfirmFocused(false);
                setTouchedConfirm(true);
              }}
              autoComplete="new-password"
              className="mt-1"
              invalid={confirmAllErrs.length > 0}
              errorId={confirmAllErrs.length ? "confirm-password-error" : undefined}
              disabled={!pwdOk}
              aria-disabled={!pwdOk}
              readOnly={!pwdOk}
              tabIndex={pwdOk ? 0 : -1}
            />
            {!pwdOk ? (
              <p className="text-sm opacity-80 mt-1"></p>
            ) : confirmAllErrs.length ? (
              <FormErrorText id="confirm-password-error" aria-live="polite">
                {confirmAllErrs.join(" ")}
              </FormErrorText>
            ) : null}
          </div>

          <div className="flex items-center gap-2">
            <Checkbox
              id="showPasswords"
              checked={showPasswords}
              onChange={(e) => setShowPasswords(e.currentTarget.checked)}
            />
            <Label htmlFor="showPasswords" size="sm">Show passwords</Label>
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
