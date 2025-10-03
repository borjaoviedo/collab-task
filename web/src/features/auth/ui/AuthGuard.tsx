import { Navigate, Outlet } from "react-router-dom";
import { useAuthStore } from "@shared/store/auth.store";

export function AuthGuard() {
  const isAuth = useAuthStore((s) => s.isAuthenticated);

  if (!isAuth) {
    return <Navigate to="/login" replace />;
  }

  return <Outlet />;
}
