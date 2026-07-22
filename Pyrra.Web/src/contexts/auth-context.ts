import { createContext } from 'react'
import type { UserResponse } from '../types/auth'

// O objeto de contexto e seu tipo moram FORA do AuthContext.tsx de propósito.
//
// A regra react-refresh/only-export-components exige que um arquivo com componente
// exporte apenas componentes. O AuthContext.tsx exporta o AuthProvider, então não
// pode exportar também o contexto — e `allowConstantExport` (ativo via
// reactRefresh.configs.vite) não ajuda aqui, porque só libera literais, não o
// retorno de createContext().
//
// Este módulo não exporta componente nenhum, então a regra não se aplica a ele, e
// tanto o provider quanto o hook podem importar daqui.

export interface AuthContextValue {
  user: UserResponse | null
  /** true enquanto a sessão salva ainda está sendo verificada no backend. */
  loading: boolean
  login: (email: string, password: string) => Promise<void>
  /** Cria a conta e já deixa o usuário autenticado — não exige login depois. */
  register: (name: string, email: string, password: string) => Promise<void>
  logout: () => void
}

// undefined como default é o que permite ao useAuth detectar uso fora do provider.
export const AuthContext = createContext<AuthContextValue | undefined>(undefined)
