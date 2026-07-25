import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'

// Gate do onboarding: só deixa o usuário entrar no app (AppLayout) depois de ter
// concluído ou pulado o onboarding de primeiro acesso. Fica ANINHADO dentro do
// ProtectedRoute — a sessão já está garantida aqui — e por FORA da rota
// /onboarding, para a própria tela de onboarding não entrar em loop de redirect.
export function RequireOnboarding() {
  const { user } = useAuth()

  if (user && !user.onboardingCompleted) {
    return <Navigate to="/onboarding" replace />
  }

  return <Outlet />
}

export default RequireOnboarding
