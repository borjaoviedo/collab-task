import { ThemeSelector } from "@shared/ui/ThemeSelector";
import { useTheme } from "@shared/hooks/useTheme";
import { Button } from "@shared/ui/Button";

export function ThemePanel() {
  const { mode, palette, setThemeMode, setThemePalette } = useTheme();

  return (
    <div className="theme-controls" role="group" aria-label="Theme controls">
      <ThemeSelector value={palette} onChange={setThemePalette} />
      <div className="mode-segment" role="group" aria-label="Theme mode">
        <Button
          variant="outline"
          size="sm"
          data-active={(mode === "light") || undefined}
          aria-pressed={mode === "light"}
          onClick={() => setThemeMode("light")}
          aria-label="Light mode"
          type="button"
        >
          Light
        </Button>
        <Button
          variant="outline"
          size="sm"
          data-active={(mode === "dark") || undefined}
          aria-pressed={mode === "dark"}
          onClick={() => setThemeMode("dark")}
          aria-label="Dark mode"
          type="button"
        >
          Dark
        </Button>
      </div>
    </div>
  );
}
