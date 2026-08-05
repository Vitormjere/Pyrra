import { createContext } from 'react'
import type { HubConnection } from '@microsoft/signalr'

// conexão SignalR compartilhada do chat, null enquanto não há sessão ou ela ainda não iniciou
export const ChatConnectionContext = createContext<HubConnection | null>(null)
