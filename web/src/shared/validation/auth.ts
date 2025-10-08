export function isValidEmail(v: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v);
}

export function isValidUserName(value: string): string | null {
  if (value.trim().length === 0) return "User name cannot be empty";
  const v = value.trim();
  if (v.length < 2 || v.length > 100) return "User name must be between 2 and 100 characters";
  if (/[^\p{L}\s]/u.test(v)) return "User name must contain only letters and spaces";
  if (/\s{2,}/.test(v)) return "User name cannot contain consecutive spaces";
  return null;
}

export function passwordError(v: string): string | null {
  if (v.length < 8) return "Password must be at least 8 characters";
  if (!/[A-Z]/.test(v)) return "Password must include at least one uppercase letter";
  if (!/[0-9]/.test(v)) return "Password must include at least one number";
  if (!/[^A-Za-z0-9]/.test(v)) return "Password must include at least one special character";
  return null;
}

export type FieldErrorMap = Record<string, string[]>;

export type UiErrorLike =
  | {
      status?: number;
      details?: unknown;
      title?: string;
      message?: string;
    }
  | null
  | undefined;

export function normalizeServerFieldErrors(uiError: UiErrorLike): FieldErrorMap {
  if (!uiError || uiError.status !== 422) return {};
  const d = uiError.details;
  if (!d || typeof d !== "object") return {};
  const details = d as Record<string, string | string[]>;
  const norm: FieldErrorMap = {};
  for (const [k, v] of Object.entries(details)) {
    norm[k.toLowerCase()] = Array.isArray(v) ? v.map(String) : [String(v)];
  }
  return norm;
}
