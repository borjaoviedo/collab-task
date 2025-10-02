import { useEffect, useState, useCallback } from "react";

export type ThemeMode = "light" | "dark" | "system";
export type Palette = "neutral" | "warm" | "cool";

const LS_MODE = "ui_theme_mode";
const LS_PALETTE = "ui_theme_palette";

function applyAttributes(mode: ThemeMode, palette: Palette) {
  const root = document.documentElement;

  // Resolve dark/light when mode === 'system'
  const mql = window.matchMedia("(prefers-color-scheme: dark)");
  const effectiveDark = mode === "dark" || (mode === "system" && mql.matches);
  root.setAttribute("data-theme", effectiveDark ? "dark" : "light");
  root.setAttribute("data-palette", palette);
}

export function useTheme(initial?: { mode?: ThemeMode; palette?: Palette }) {
  const [mode, setMode] = useState<ThemeMode>(() => {
    return (localStorage.getItem(LS_MODE) as ThemeMode) || initial?.mode || "system";
  });
  const [palette, setPalette] = useState<Palette>(() => {
    return (localStorage.getItem(LS_PALETTE) as Palette) || initial?.palette || "neutral";
  });

  // Apply on mount and when changes happen
  useEffect(() => {
    applyAttributes(mode, palette);
    localStorage.setItem(LS_MODE, mode);
    localStorage.setItem(LS_PALETTE, palette);
  }, [mode, palette]);

  // React to OS theme changes when on 'system'
  useEffect(() => {
    if (mode !== "system") return;
    const mql = window.matchMedia("(prefers-color-scheme: dark)");
    const handler = () => applyAttributes("system", palette);
    mql.addEventListener("change", handler);
    return () => mql.removeEventListener("change", handler);
  }, [mode, palette]);

  const setThemeMode = useCallback((m: ThemeMode) => setMode(m), []);
  const setThemePalette = useCallback((p: Palette) => setPalette(p), []);

  return { mode, palette, setThemeMode, setThemePalette };
}
