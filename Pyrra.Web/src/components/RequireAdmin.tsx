import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'

// Gate das rotas administrativas (/admin/contas, /admin/solicitacoes, /admin/mensagens) — quem
// não é admin e tentar acessar direto pela URL é levado para /hoje, mesmo critério de qualquer
// outra rota fora do alcance da conta.
export function RequireAdmin() {
  const { user } = useAuth()

  if (!user?.isAdmin) {
    return <Navigate to="/hoje" replace />
  }

  return <Outlet />
}

export default RequireAdmin
