import { useContext } from 'react'
import { AuthContext } from '../contexts/auth-context'
import type { AuthContextValue } from '../contexts/auth-context'

// acesso à sessão atual, em arquivo próprio pra não quebrar o fast refresh do provider
export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error('useAuth precisa ser usado dentro de um <AuthProvider>.')
  }
  return context
}

export default useAuth
