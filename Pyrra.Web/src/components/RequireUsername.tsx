import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'

// Gate do username: só deixa entrar no app quem já escolheu um username. Fica ANINHADO dentro do
// RequireOnboarding (a sessão e o onboarding já estão garantidos aqui) e por FORA da rota
// /username, para a própria tela de escolha não entrar em loop de redirect.
//
// Cobre novos e antigos com um caminho só: usuários criados antes da feature de Amigos têm
// username null e são levados à escolha no primeiro acesso seguinte.
export function RequireUsername() {
  const { user } = useAuth()

  if (user && !user.username) {
    return <Navigate to="/username" replace />
  }

  return <Outlet />
}

export default RequireUsername
