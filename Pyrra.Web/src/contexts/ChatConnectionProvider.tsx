import { useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import { HubConnectionState } from '@microsoft/signalr'
import { createChatConnection } from '../services/signalr'
import { useAuth } from '../hooks/useAuth'
import { ChatConnectionContext } from './chat-connection-context'

// Dona da ÚNICA conexão SignalR da sessão (Fase Admin-4b) — inicia quando há usuário logado, para
// no logout. Reconexão automática já vem do withAutomaticReconnect() do client (createChatConnection),
// não precisa de lógica manual aqui. Fica acima de ChatUnreadProvider e de qualquer ChatPanel, que
// consomem a MESMA conexão via useChatConnection() e cada um registra seus próprios handlers com
// connection.on(...) — o client do SignalR já suporta múltiplos listeners por evento.
export function ChatConnectionProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth()
  const [connection, setConnection] = useState<HubConnection | null>(null)

  useEffect(() => {
    if (!user) return

    const hubConnection = createChatConnection()
    let active = true

    hubConnection
      .start()
      .then(() => {
        if (active) setConnection(hubConnection)
      })
      .catch(() => {
        // Sem sessão de tempo real: o REST (envio/recebimento via reload) continua funcionando
        // normalmente, é só o push instantâneo que não chega até a conexão se restabelecer.
      })

    return () => {
      active = false
      setConnection(null)
      if (hubConnection.state !== HubConnectionState.Disconnected) {
        void hubConnection.stop()
      }
    }
  }, [user])

  return <ChatConnectionContext.Provider value={connection}>{children}</ChatConnectionContext.Provider>
}

export default ChatConnectionProvider
