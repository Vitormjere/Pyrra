import { useContext } from 'react'
import { ChatUnreadContext } from '../contexts/chat-unread-context'
import type { ChatUnreadContextValue } from '../contexts/chat-unread-context'

// Contagem de mensagens de chat não lidas + refresh. Em arquivo próprio, separado do provider,
// para o Fast Refresh continuar funcionando no arquivo do provider.
export function useChatUnread(): ChatUnreadContextValue {
  const context = useContext(ChatUnreadContext)
  if (context === undefined) {
    throw new Error('useChatUnread precisa ser usado dentro de um <ChatUnreadProvider>.')
  }
  return context
}

export default useChatUnread
