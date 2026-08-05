import { useCallback, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { getChatConversations } from '../services/chatService'
import { useAuth } from '../hooks/useAuth'
import { useChatConnection } from '../hooks/useChatConnection'
import { ChatUnreadContext } from './chat-unread-context'

// fica acima do RootLayout pro badge do menu e a tela de chat lerem a mesma contagem
export function ChatUnreadProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth()
  const connection = useChatConnection()
  const [count, setCount] = useState(0)

  const refresh = useCallback(async () => {
    try {
      const conversations = await getChatConversations()
      setCount(conversations.reduce((total, c) => total + c.unreadCount, 0))
    } catch {
      // badge é secundário, erro aqui não deve estourar na tela
    }
  }, [])

  useEffect(() => {
    if (!user) return
    let active = true

    async function run() {
      try {
        const conversations = await getChatConversations()
        if (active) setCount(conversations.reduce((total, c) => total + c.unreadCount, 0))
      } catch {
        // mesmo critério do refresh manual, falha aqui não aparece pro usuário
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [user])

  // mensagem recebida via hub sobe o badge na hora, reconsulta em vez de incrementar pra nunca duplicar contagem
  useEffect(() => {
    if (!connection) return

    // off() precisa da referência exata do handler, senão remove todos os listeners de "ReceiveMessage" (inclusive o do ChatPanel)
    function handleReceiveMessage() {
      void refresh()
    }

    connection.on('ReceiveMessage', handleReceiveMessage)

    return () => {
      connection.off('ReceiveMessage', handleReceiveMessage)
    }
  }, [connection, refresh])

  const value = useMemo(() => ({ count, refresh }), [count, refresh])

  return <ChatUnreadContext.Provider value={value}>{children}</ChatUnreadContext.Provider>
}

export default ChatUnreadProvider
