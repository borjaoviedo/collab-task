import { forwardRef } from "react";
import { Slot } from "@radix-ui/react-slot";
import type { ButtonHTMLAttributes } from "react";

type Variant = "primary" | "secondary" | "success" | "warning" | "danger" | "outline";
type Size = "sm" | "md" | "lg";
type Elev = 0 | 1 | 2;

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
  size?: Size;
  elev?: Elev;
  asChild?: boolean;
  isLoading?: boolean;
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(function Button(
  {
    variant = "primary",
    size = "md",
    elev = 0,
    className = "",
    asChild = false,
    isLoading = false,
    type = "button",
    disabled,
    ...props
  },
  ref
) {
  const classes = [
    "btn",
    `btn-${variant}`,
    `btn-${size}`,
    `btn-elev-${elev}`,
    isLoading ? "btn-loading" : "",
    className,
  ]
    .filter(Boolean)
    .join(" ");

  if (asChild) {
    // No pasamos ref porque Slot no expone tipo de ref HTMLButtonElement
    return <Slot className={classes} aria-busy={isLoading || undefined} {...props} />;
  }

  return (
    <button
      ref={ref}
      className={classes}
      type={type}
      aria-busy={isLoading || undefined}
      disabled={disabled || isLoading || undefined}
      {...props}
    />
  );
});
