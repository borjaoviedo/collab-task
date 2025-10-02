import type { HTMLAttributes } from "react";

type Props = HTMLAttributes<HTMLParagraphElement>;

export function FormErrorText({ className = "", role = "alert", ...props }: Props) {
  return (
    <p
      role={role}
      aria-live="polite"
      className={["form-error", className].filter(Boolean).join(" ")}
      {...props}
    />
  );
}
