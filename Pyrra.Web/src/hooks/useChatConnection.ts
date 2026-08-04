import { useContext } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import { ChatConnectionContext } from '../contexts/chat-connection-context'

// Conexão SignalR compartilhada do chat (Fase Admin-4b) — null enquanto não há sessão ativa ou a
// conexão ainda não terminou de iniciar. Em arquivo próprio, mesmo motivo dos demais hooks.
export function useChatConnection(): HubConnection | null {
  return useContext(ChatConnectionContext)
}

export default useChatConnection
