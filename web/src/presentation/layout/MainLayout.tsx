import { Outlet, Link } from 'react-router-dom'

export function MainLayout() {
  return (
    <div className="min-h-dvh flex flex-col">
      <header className="border-b">
        <nav className="container mx-auto p-4 flex gap-4">
          <Link to="/">Home</Link>
          <Link to="/login">Login</Link>
        </nav>
      </header>
      <main className="container mx-auto p-4 flex-1">
        <Outlet />
      </main>
    </div>
  )
}
