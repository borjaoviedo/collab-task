import type { LabelHTMLAttributes, ReactNode } from "react";

type Size = "sm" | "md";

interface LabelProps extends LabelHTMLAttributes<HTMLLabelElement> {
  size?: Size;
  requiredMark?: boolean;
  children: ReactNode;
}

export function Label({
  size = "md",
  requiredMark = false,
  className = "",
  children,
  ...props
}: LabelProps) {
  const classes = ["label", `label-${size}`, className].filter(Boolean).join(" ");
  return (
    <label className={classes} {...props}>
      {children}
      {requiredMark ? (
        <>
          <span aria-hidden="true" className="label-required">*</span>
          <span className="sr-only"> required</span>
        </>
      ) : null}
    </label>
  );
}
