import { useCallback, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import * as authService from '../services/authService'
import { clearToken, getToken, setToken } from '../services/tokenStorage'
import type { UserResponse } from '../types/auth'
import { AuthContext } from './auth-context'

// Este arquivo exporta APENAS o AuthProvider. O objeto de contexto vive em
// ./auth-context e o hook useAuth em ../hooks/useAuth — a separação é o que
// mantém o Fast Refresh funcionando aqui.
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserResponse | null>(null)
  const [loading, setLoading] = useState(true)

  // Restauração da sessão ao abrir o app. Ter um token salvo não basta: ele pode
  // estar expirado, então quem decide se a sessão vale é o backend, via /auth/me.
  // Enquanto essa resposta não chega, loading segura a UI e impede a tela de login
  // de piscar para quem já estava autenticado.
  useEffect(() => {
    // Evita setState depois que o componente saiu da árvore (StrictMode monta,
    // desmonta e remonta o efeito em desenvolvimento).
    let active = true

    async function restoreSession() {
      if (!getToken()) {
        if (active) setLoading(false)
        return
      }

      try {
        const currentUser = await authService.me()
        if (active) setUser(currentUser)
      } catch {
        // 401 já foi tratado pelo interceptor; aqui só garantimos o estado local
        // coerente para os demais erros (rede, backend fora do ar).
        clearToken()
        if (active) setUser(null)
      } finally {
        if (active) setLoading(false)
      }
    }

    void restoreSession()

    return () => {
      active = false
    }
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    const auth = await authService.login({ email, password })
    setToken(auth.token)

    // Busca o usuário completo em vez de aproveitar o AuthResponse: ele traz só
    // userId/email/name, enquanto /auth/me traz timezone, tom e plano. Assim a
    // sessão recém-criada e a restaurada expõem exatamente a mesma forma de user.
    setUser(await authService.me())
  }, [])

  const logout = useCallback(() => {
    clearToken()
    setUser(null)
    // Sem navegação explícita: zerar o user já faz o ProtectedRoute redirecionar,
    // o que mantém a regra de "para onde ir" em um lugar só.
  }, [])

  const value = useMemo(
    () => ({ user, loading, login, logout }),
    [user, loading, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export default AuthProvider
