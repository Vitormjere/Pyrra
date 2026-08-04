import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'

// envolve as rotas que exigem sessão, usando <Outlet /> pra não repetir o guard em cada página
export function ProtectedRoute() {
  const { user, loading } = useAuth()

  // enquanto a sessão está sendo verificada não dá pra decidir, senão manda pro login quem tem token válido
  if (loading) {
    return null
  }

  // replace pra não empilhar no histórico, senão o botão voltar levaria de novo à rota protegida
  return user ? <Outlet /> : <Navigate to="/login" replace />
}

export default ProtectedRoute
