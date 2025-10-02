import { forwardRef } from "react";
import type { InputHTMLAttributes } from "react";

type NativeInputProps = Omit<InputHTMLAttributes<HTMLInputElement>, "size">;
type Size = "sm" | "md" | "lg";

export interface InputProps extends NativeInputProps {
  size?: Size;
  invalid?: boolean;
  errorId?: string;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(function Input(
  { size = "md", invalid = false, className = "", errorId, type = "text", ...props },
  ref
) {
  const classes = ["input", `input-${size}`, invalid ? "input-invalid" : "", className]
    .filter(Boolean)
    .join(" ");

  return (
    <input
      ref={ref}
      className={classes}
      type={type}
      aria-invalid={invalid || undefined}
      aria-describedby={errorId || props["aria-describedby"] || undefined}
      {...props}
    />
  );
});
