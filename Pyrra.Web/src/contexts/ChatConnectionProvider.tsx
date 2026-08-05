import { useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import { HubConnectionState } from '@microsoft/signalr'
import { createChatConnection } from '../services/signalr'
import { useAuth } from '../hooks/useAuth'
import { ChatConnectionContext } from './chat-connection-context'

// dona da única conexão SignalR da sessão, inicia com o login e para no logout — reconexão automática já vem do client
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
        // sem conexão em tempo real o chat ainda funciona via REST, só perde o push instantâneo
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
