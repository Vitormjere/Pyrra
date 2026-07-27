import { useContext } from 'react'
import { FriendRequestsContext } from '../contexts/friend-requests-context'
import type { FriendRequestsContextValue } from '../contexts/friend-requests-context'

// Contagem de pedidos pendentes + refresh. Em arquivo próprio, separado do provider, para o Fast
// Refresh continuar funcionando no arquivo do provider.
export function useFriendRequests(): FriendRequestsContextValue {
  const context = useContext(FriendRequestsContext)
  if (context === undefined) {
    throw new Error(
      'useFriendRequests precisa ser usado dentro de um <FriendRequestsProvider>.',
    )
  }
  return context
}

export default useFriendRequests
