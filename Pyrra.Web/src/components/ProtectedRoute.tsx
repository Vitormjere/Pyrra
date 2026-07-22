import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'

// Envolve as rotas que exigem sessão. Usa <Outlet /> para funcionar como rota de
// layout, evitando repetir o guard em cada página.
export function ProtectedRoute() {
  const { user, loading } = useAuth()

  // Enquanto a sessão salva está sendo verificada não dá para decidir: redirecionar
  // aqui jogaria para o login quem tem token válido, só porque a resposta do
  // /auth/me ainda não voltou.
  if (loading) {
    return null
  }

  // replace para o login não empilhar no histórico — o botão "voltar" levaria de
  // novo à rota protegida, que redirecionaria outra vez.
  return user ? <Outlet /> : <Navigate to="/login" replace />
}

export default ProtectedRoute
