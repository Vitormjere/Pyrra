import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'

// gate das rotas administrativas — quem não é admin e tenta acessar direto pela URL vai pra /hoje
export function RequireAdmin() {
  const { user } = useAuth()

  if (!user?.isAdmin) {
    return <Navigate to="/hoje" replace />
  }

  return <Outlet />
}

export default RequireAdmin
