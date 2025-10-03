import { ThemeSelector } from "@shared/ui/ThemeSelector";
import { useTheme, type ThemeMode } from "@shared/hooks/useTheme";
import { Button } from "@shared/ui/Button";

export function ThemeControls() {
  const { mode, palette, setThemeMode, setThemePalette } = useTheme();

  const ModeBtn = ({ m, label }: { m: ThemeMode; label: string }) => (
    <Button
      variant="outline"
      size="sm"
      data-active={(mode === m) || undefined}
      aria-pressed={mode === m}
      onClick={() => setThemeMode(m)}
      aria-label={`${label} mode`}
      type="button"
    >
      {label}
    </Button>
  );

  return (
    <div className="theme-controls" role="group" aria-label="Theme controls">
      <ThemeSelector value={palette} onChange={setThemePalette} />
      <div className="mode-segment" role="group" aria-label="Theme mode">
        <ModeBtn m="light" label="Light" />
        <ModeBtn m="dark" label="Dark" />
      </div>
    </div>
  );
}
