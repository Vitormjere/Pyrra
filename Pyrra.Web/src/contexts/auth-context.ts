import { createContext } from 'react'
import type { UserResponse } from '../types/auth'

// contexto e tipo ficam fora do AuthContext.tsx porque o eslint react-refresh só deixa um arquivo com componente exportar componentes
export interface AuthContextValue {
  user: UserResponse | null
  // true enquanto a sessão salva ainda está sendo verificada no backend
  loading: boolean
  login: (email: string, password: string) => Promise<void>
  // já deixa o usuário autenticado, não precisa logar de novo depois de cadastrar
  register: (name: string, email: string, password: string, captchaToken: string) => Promise<void>
  // cria, vincula ou só loga — o backend decide qual dos três, ver AuthService.LoginWithGoogleAsync
  loginWithGoogle: (idToken: string) => Promise<void>
  // recarrega o usuário do servidor, usado após editar preferências
  refreshUser: () => Promise<void>
  // substitui o usuário do contexto por uma versão já conhecida, sem precisar de um GET a mais
  applyUser: (user: UserResponse) => void
  logout: () => void
}

// undefined como default é o que permite ao useAuth detectar uso fora do provider
export const AuthContext = createContext<AuthContextValue | undefined>(undefined)
