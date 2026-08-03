import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'

// Gate das seções operacionais do dia a dia (Hoje, Zelo, Agenda, Treino, Tarefas, Finanças,
// Nutrição, Diário, Amigos) — contas admin não têm essas seções no menu (ver AdminLayout) e,
// se tentarem acessar direto pela URL, são levadas para /times, a primeira seção do menu admin.
// Mesma posição estrutural de RequireOnboarding/RequireUsername: aninhado dentro do RootLayout,
// que já garantiu sessão/onboarding/username.
export function RequireNotAdmin() {
  const { user } = useAuth()

  if (user?.isAdmin) {
    return <Navigate to="/times" replace />
  }

  return <Outlet />
}

export default RequireNotAdmin
