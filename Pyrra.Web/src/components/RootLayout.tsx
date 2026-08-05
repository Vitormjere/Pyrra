import AdminLayout from './AdminLayout'
import AppLayout from './AppLayout'
import { useAuth } from '../hooks/useAuth'

// escolhe o layout conforme o papel da conta — monta uma vez só e não remonta ao navegar, já que isAdmin não muda no meio da sessão
export function RootLayout() {
  const { user } = useAuth()
  return user?.isAdmin ? <AdminLayout /> : <AppLayout />
}

export default RootLayout
