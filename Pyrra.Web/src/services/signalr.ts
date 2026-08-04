import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import type { HubConnection } from '@microsoft/signalr'
import { baseURL } from './api'
import { getToken } from './tokenStorage'

// Canal de tempo real do chat (Fase Admin-4b) — mesma origem da API REST, só troca o path.
// accessTokenFactory é chamado a cada tentativa de conexão (inicial e cada reconexão), então lê o
// token atual do storage, não um valor fixado na hora da criação.
export function createChatConnection(): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(`${baseURL}/hubs/chat`, { accessTokenFactory: () => getToken() ?? '' })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build()
}
