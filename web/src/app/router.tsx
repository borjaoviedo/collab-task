import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import MainLayout from '@presentation/layout/MainLayout';
import { AuthGuard } from '@features/auth/ui/AuthGuard';
import { HomePage } from '@presentation/routes/HomePage';
import { RouteErrorPage } from '@presentation/routes/RouteErrorPage';
import { LoginPage } from '@features/auth/ui/LoginPage';
import { RegisterPage } from '@features/auth/ui/RegisterPage';
import { MePage } from '@features/auth/ui/MePage';
import ProjectsPage from '@features/projects/ui/ProjectsPage';
import ProjectBoardPage from '@features/projects/ui/ProjectBoardPage';
import ProjectMembersPage from '@features/members/ui/ProjectMembersPage';

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
        children: [
          { path: 'me', element: <MePage /> },
          { path: 'projects', element: <ProjectsPage/>},
          { path: "/projects/:id", element: <ProjectBoardPage/>},
          { path: "/projects/:id/members", element: <ProjectMembersPage/>},
        ],
      },
    ],
  },
]);

export function AppRouter() {
  return <RouterProvider router={router} />;
}
