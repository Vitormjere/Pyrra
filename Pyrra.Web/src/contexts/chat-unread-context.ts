import { createContext } from 'react'

// Total de mensagens de chat não lidas (soma de todas as conversas), compartilhado entre o badge
// do menu ("Suporte" no AppLayout, "Mensagens" no AdminLayout) e as próprias telas de chat, que
// chamam refresh() depois de marcar uma conversa como lida. Mesmo motivo dos demais contexts
// deste diretório: arquivo próprio, separado do provider, para o Fast Refresh continuar
// funcionando no arquivo do provider.
export interface ChatUnreadContextValue {
  count: number
  refresh: () => Promise<void>
}

export const ChatUnreadContext = createContext<ChatUnreadContextValue | undefined>(undefined)
