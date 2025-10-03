export function isValidEmail(v: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v);
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
