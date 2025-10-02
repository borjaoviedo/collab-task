import { Link, Outlet } from "react-router-dom";
import { ThemeControls } from "@features/theme/ui/ThemeControls";
import { useAuthStore } from "@shared/store/auth.store";

export default function MainLayout() {
  const isAuth = useAuthStore((s) => s.isAuthenticated);

  return (
    <div className="min-h-dvh flex flex-col text-[color:var(--color-foreground)] bg-app-gradient">
      <header className="flex w-full items-center justify-between px-6 py-4">
        <Link to="/" className="text-xl font-semibold">CollabTask</Link>
        <nav className="flex items-center gap-4">
          {isAuth ? (
            <>
            <Link to="/me" className="underline">Profile</Link>
            <Link to="/settings" className="underline">Settings</Link>
            </>
          ) : null}
          <ThemeControls />
        </nav>
      </header>

      <main className="flex-1 w-full grid place-items-center min-h-0">
        <Outlet />
      </main>

      <footer className="w-full px-6 py-6 text-sm text-[color:var(--color-muted-foreground)]">
        © CollabTask
      </footer>
    </div>
  );
}
