import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import MainLayout from '@presentation/layout/MainLayout';
import { AuthGuard } from '@features/auth/ui/AuthGuard';
import { HomePage } from '@presentation/routes/HomePage';
import { RouteErrorPage } from '@presentation/routes/RouteErrorPage';
import { LoginPage } from '@features/auth/ui/LoginPage';
import { RegisterPage } from '@features/auth/ui/RegisterPage';
import { MePage } from '@features/auth/ui/MePage';

const router = createBrowserRouter([
  {
    element: <MainLayout />,
    errorElement: <RouteErrorPage />,
    children: [
      { index: true, element: <HomePage /> },
      { path: 'login', element: <LoginPage /> },
      { path: 'register', element: <RegisterPage /> },
      {
        element: <AuthGuard />,
        children: [{ path: 'me', element: <MePage /> }],
      },
    ],
  },
]);

export function AppRouter() {
  return <RouterProvider router={router} />;
}
