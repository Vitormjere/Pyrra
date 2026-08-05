import { useContext } from 'react'
import { FriendRequestsContext } from '../contexts/friend-requests-context'
import type { FriendRequestsContextValue } from '../contexts/friend-requests-context'

// contagem de pedidos pendentes + refresh, em arquivo próprio pra não quebrar o fast refresh do provider
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
