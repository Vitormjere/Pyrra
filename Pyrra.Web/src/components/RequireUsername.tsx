import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'

// gate do username (aninhado no RequireOnboarding, fora de /username) — cobre contas antigas, que caem aqui com username null
export function RequireUsername() {
  const { user } = useAuth()

  if (user && !user.username) {
    return <Navigate to="/username" replace />
  }

  return <Outlet />
}

export default RequireUsername
