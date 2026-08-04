import { useCallback, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { getChatConversations } from '../services/chatService'
import { useAuth } from '../hooks/useAuth'
import { ChatUnreadContext } from './chat-unread-context'

// Provider do total de mensagens não lidas. Mesmo papel do TeamInvitesProvider: fica ACIMA do
// RootLayout (admin e jogador), pro badge do menu e a tela de chat lerem a mesma contagem.
export function ChatUnreadProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth()
  const [count, setCount] = useState(0)

  const refresh = useCallback(async () => {
    try {
      const conversations = await getChatConversations()
      setCount(conversations.reduce((total, c) => total + c.unreadCount, 0))
    } catch {
      // Silencioso: o badge é secundário, um erro aqui não deve estourar na tela.
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
        // Silencioso, mesmo critério do refresh manual.
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [user])

  const value = useMemo(() => ({ count, refresh }), [count, refresh])

  return <ChatUnreadContext.Provider value={value}>{children}</ChatUnreadContext.Provider>
}

export default ChatUnreadProvider
