import { useCallback, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import * as authService from '../services/authService'
import { clearToken, getToken, setToken } from '../services/tokenStorage'
import type { AuthResponse, UserResponse } from '../types/auth'
import { applyAccentColor } from '../utils/accentColors'
import { AuthContext } from './auth-context'

// contexto e hook ficam em arquivos separados pra não quebrar o fast refresh
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserResponse | null>(null)
  const [loading, setLoading] = useState(true)

  // token salvo não garante sessão válida, então confirma com /auth/me antes de liberar a UI
  useEffect(() => {
    // evita setState depois que o componente desmontou (StrictMode remonta o efeito em dev)
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
        // 401 já é tratado pelo interceptor, aqui só limpamos o estado pros outros erros
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

  // aplica a cor de destaque assim que o usuário é conhecido (login, cadastro, restauração de
  // sessão, refreshUser, applyUser — todos passam por setUser) e volta pro verde padrão no
  // logout/deslogado, pra telas públicas (login, cadastro) não ficarem com a cor de uma sessão
  // anterior
  useEffect(() => {
    applyAccentColor(user?.accentColor ?? 'Verde')
  }, [user])

  // login e cadastro caem aqui pra buscar o usuário completo via /auth/me em vez do AuthResponse enxuto
  const startSession = useCallback(async (auth: AuthResponse) => {
    setToken(auth.token)
    setUser(await authService.me())
  }, [])

  const login = useCallback(
    async (email: string, password: string) => {
      await startSession(await authService.login({ email, password }))
    },
    [startSession],
  )

  const register = useCallback(
    async (name: string, email: string, password: string, captchaToken: string) => {
      await startSession(await authService.register({ name, email, password, captchaToken }))
    },
    [startSession],
  )

  const loginWithGoogle = useCallback(
    async (idToken: string) => {
      await startSession(await authService.loginWithGoogle(idToken))
    },
    [startSession],
  )

  const refreshUser = useCallback(async () => {
    setUser(await authService.me())
  }, [])

  // aplica um usuário já conhecido (ex: resposta do onboarding) sem ir ao servidor de novo
  const applyUser = useCallback((next: UserResponse) => {
    setUser(next)
  }, [])

  const logout = useCallback(() => {
    clearToken()
    setUser(null)
    // zerar o user já faz o ProtectedRoute redirecionar, sem precisar navegar na mão
  }, [])

  const value = useMemo(
    () => ({ user, loading, login, register, loginWithGoogle, refreshUser, applyUser, logout }),
    [user, loading, login, register, loginWithGoogle, refreshUser, applyUser, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export default AuthProvider
