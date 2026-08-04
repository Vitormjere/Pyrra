import { createContext } from 'react'
import type { HubConnection } from '@microsoft/signalr'

// Conexão SignalR compartilhada do chat (Fase Admin-4b) — arquivo próprio, separado do provider,
// mesmo motivo dos demais contexts deste diretório (Fast Refresh). null enquanto não há sessão ou
// a conexão ainda não terminou de iniciar; ChatUnreadProvider e ChatPanel tratam esse caso
// simplesmente não se inscrevendo em nenhum evento.
export const ChatConnectionContext = createContext<HubConnection | null>(null)
