import { createContext } from 'react'

// Contagem de pedidos de amizade pendentes, compartilhada entre o badge do menu e a tela de
// Amigos (que chama refresh após aceitar/recusar). Mora em arquivo próprio, separado do provider,
// pelo mesmo motivo do auth-context: manter o Fast Refresh funcionando no arquivo do provider.
export interface FriendRequestsContextValue {
  count: number
  refresh: () => Promise<void>
}

export const FriendRequestsContext = createContext<FriendRequestsContextValue | undefined>(
  undefined,
)

// Chave do convite guardado quando alguém abre um link de convite deslogado — consumido depois
// que a sessão existe.
export const PENDING_INVITE_KEY = 'pyrra.pendingInvite'
