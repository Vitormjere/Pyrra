import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import type { HubConnection } from '@microsoft/signalr'
import { baseURL } from './api'
import { getToken } from './tokenStorage'

// canal de tempo real do chat, mesma origem da API REST — accessTokenFactory lê o token atual do storage a cada tentativa de conexão
export function createChatConnection(): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(`${baseURL}/hubs/chat`, { accessTokenFactory: () => getToken() ?? '' })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build()
}
