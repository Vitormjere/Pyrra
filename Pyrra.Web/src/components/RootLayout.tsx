import AdminLayout from './AdminLayout'
import AppLayout from './AppLayout'
import { useAuth } from '../hooks/useAuth'

// Escolhe o chrome (sidebar/menu) conforme o papel da conta — AdminLayout para IsAdmin=true,
// AppLayout para o resto, como sempre foi. Fica como elemento de UMA rota de layout só (sem
// path), envolvendo TODAS as rotas protegidas (operacionais, compartilhadas e admin): assim ele
// monta uma vez por sessão e não remonta ao navegar entre seções, já que user.isAdmin não muda
// no meio da sessão. A restrição de QUAIS rotas cada papel alcança fica nos guards
// RequireAdmin/RequireNotAdmin, não aqui.
export function RootLayout() {
  const { user } = useAuth()
  return user?.isAdmin ? <AdminLayout /> : <AppLayout />
}

export default RootLayout
