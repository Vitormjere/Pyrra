import { useContext } from 'react'
import { ChatUnreadContext } from '../contexts/chat-unread-context'
import type { ChatUnreadContextValue } from '../contexts/chat-unread-context'

// contagem de mensagens não lidas + refresh, em arquivo próprio pra não quebrar o fast refresh do provider
export function useChatUnread(): ChatUnreadContextValue {
  const context = useContext(ChatUnreadContext)
  if (context === undefined) {
    throw new Error('useChatUnread precisa ser usado dentro de um <ChatUnreadProvider>.')
  }
  return context
}

export default useChatUnread
