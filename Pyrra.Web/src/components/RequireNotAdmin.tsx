import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'

// gate das seções operacionais do dia a dia — contas admin não têm elas no menu e, se tentarem acessar direto pela URL, vão pra /times
export function RequireNotAdmin() {
  const { user } = useAuth()

  if (user?.isAdmin) {
    return <Navigate to="/times" replace />
  }

  return <Outlet />
}

export default RequireNotAdmin
