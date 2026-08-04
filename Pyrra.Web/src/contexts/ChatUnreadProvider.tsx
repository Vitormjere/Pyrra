import { useCallback, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { getChatConversations } from '../services/chatService'
import { useAuth } from '../hooks/useAuth'
import { useChatConnection } from '../hooks/useChatConnection'
import { ChatUnreadContext } from './chat-unread-context'

// Provider do total de mensagens não lidas. Mesmo papel do TeamInvitesProvider: fica ACIMA do
// RootLayout (admin e jogador), pro badge do menu e a tela de chat lerem a mesma contagem.
export function ChatUnreadProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth()
  const connection = useChatConnection()
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

  // Tempo real (Fase Admin-4b): qualquer mensagem recebida via Hub sobe o badge na hora, sem
  // precisar navegar/recarregar. Reconsulta em vez de incrementar na mão — mais simples e sempre
  // correto, evita duplicar contagem se o ChatPanel já estiver marcando como lida ao mesmo tempo.
  useEffect(() => {
    if (!connection) return

    // A mesma conexão é compartilhada com o ChatPanel que estiver aberto (cada um registra seu
    // próprio handler via connection.on). off() PRECISA da referência exata do handler — chamado
    // só com o nome do evento, ele removeria TODOS os handlers de "ReceiveMessage", inclusive o
    // do ChatPanel.
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
