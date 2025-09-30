import { createBrowserRouter, RouterProvider } from 'react-router-dom'
import { MainLayout } from "@presentation/layout/MainLayout"
// import { HomePage } from '@presentation/routes'
// import { NotFoundPage } from '@presentation/routes/not-found'
// import { LoginPage } from '@features/auth/ui/LoginPage'
// import { RegisterPage } from '@features/auth/ui/RegisterPage'

const router = createBrowserRouter([
  {
    path: '/',
    element: <MainLayout />,
    children: [
      // { index: true, element: <HomePage /> },
      // { path: 'login', element: <LoginPage /> },
      // { path: 'register', element: <RegisterPage /> },
      // { path: '*', element: <NotFoundPage /> },
    ],
  },
])

export function AppRouter() {
  return <RouterProvider router={router} />
}
