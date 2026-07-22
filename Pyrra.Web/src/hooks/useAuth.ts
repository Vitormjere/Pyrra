import { useContext } from 'react'
import { AuthContext } from '../contexts/auth-context'
import type { AuthContextValue } from '../contexts/auth-context'

// Acesso à sessão atual. Fica em arquivo próprio, separado do AuthProvider, para
// o Fast Refresh continuar funcionando no arquivo do provider.
export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error('useAuth precisa ser usado dentro de um <AuthProvider>.')
  }
  return context
}

export default useAuth
