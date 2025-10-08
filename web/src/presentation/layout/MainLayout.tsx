import { Link, Outlet, useLocation } from "react-router-dom";
import { ThemeControls } from "@features/theme/ui/ThemeControls";
import { useAuthStore } from "@shared/store/auth.store";
import { isSystemAdmin, normalizeSysRole } from "@features/users/domain/User";

export default function MainLayout() {
  const isAuth = useAuthStore((s) => s.isAuthenticated);

  return (
    <div className="min-h-dvh flex flex-col text-[color:var(--color-foreground)] bg-app-gradient">
      <Header isAuth={isAuth} />
      <main className="flex-1 w-full grid place-items-center min-h-0">
        <Outlet />
      </main>
      <footer className="w-full px-6 py-6 text-sm text-[color:var(--color-muted-foreground)]">
        © CollabTask
      </footer>
    </div>
  );
}

export function Header({ isAuth }: { isAuth: boolean }) {
  const { pathname } = useLocation();
  const logout = useAuthStore((s) => s.logout);
  const roleRaw = useAuthStore((s) => s.profile?.role); 
  const role = normalizeSysRole(roleRaw);

  return (
    <header className="flex w-full items-center justify-between px-6 py-4">
      <Link to="/" className="text-xl font-semibold">
        CollabTask
      </Link>
      <nav className="flex items-center gap-4">
        {isAuth && (
          <>
            {pathname !== "/projects" && (
              <Link to="/projects" className="underline">
                Projects
              </Link>
            )}
            {pathname !== "/me" && (
              <Link to="/me" className="underline">
                Profile
              </Link>
            )}
            {isSystemAdmin(role ?? "User") && pathname !== "/users" && (
              <Link to="/users" className="underline">
                User management
              </Link>
            )}
            <Link to="/" className="underline" onClick={logout}>
              Sign out
            </Link>
          </>
        )}
        <ThemeControls />
      </nav>
    </header>
  );
}
