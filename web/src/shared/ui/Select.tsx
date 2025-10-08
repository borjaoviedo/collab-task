import { forwardRef } from "react";
import type { SelectHTMLAttributes } from "react";

type Props = SelectHTMLAttributes<HTMLSelectElement>;

export const Select = forwardRef<HTMLSelectElement, Props>(function Select(
  { className = "", ...props },
  ref
) {
  return (
    <div className="relative">
      <select
        ref={ref}
        className={`w-full border rounded px-2 py-2 h-10 pr-8 appearance-none ${className}`}
        {...props}
      />
      <span className="pointer-events-none absolute right-2 top-1/2 -translate-y-1/2 text-slate-500">
        ▾
      </span>
    </div>
  );
});
