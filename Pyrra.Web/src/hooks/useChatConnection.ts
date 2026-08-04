import { useContext } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import { ChatConnectionContext } from '../contexts/chat-connection-context'

// conexão SignalR compartilhada do chat, null enquanto não há sessão ativa ou ela ainda não iniciou
export function useChatConnection(): HubConnection | null {
  return useContext(ChatConnectionContext)
}

export default useChatConnection
