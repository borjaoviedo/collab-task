import type { MouseEvent } from "react";
import { Button } from "@shared/ui/Button";

export type Palette = "neutral" | "warm" | "cool";

type Props = {
  value: Palette;
  onChange: (p: Palette) => void;
};

export function ThemeSelector({ value, onChange }: Props) {
  const opts: Palette[] = ["neutral", "warm", "cool"];

  function handleClick(p: Palette) {
    return (e: MouseEvent<HTMLButtonElement>) => {
      e.preventDefault();
      onChange(p);
    };
  }

  return (
    <div className="flex items-center gap-3" role="group" aria-label="Color palette">
      {opts.map((p) => {
        const active = value === p;
        return (
          <Button
            key={p}
            asChild
            size="sm"
            data-active={active || undefined}
            aria-current={active ? "true" : undefined}
            aria-label={`Switch to ${p} palette`}
            onClick={handleClick(p)}
          >
            <span className={`theme-option theme-option-${p}`} />
          </Button>
        );
      })}
    </div>
  );
}
