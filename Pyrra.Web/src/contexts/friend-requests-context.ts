import { createContext } from 'react'

// contagem de pedidos de amizade pendentes, compartilhada entre o badge do menu e a tela de Amigos
export interface FriendRequestsContextValue {
  count: number
  refresh: () => Promise<void>
}

export const FriendRequestsContext = createContext<FriendRequestsContextValue | undefined>(
  undefined,
)

// chave do convite guardado quando alguém abre um link de convite deslogado, consumido depois que a sessão existe
export const PENDING_INVITE_KEY = 'pyrra.pendingInvite'
