import { createContext } from 'react'

// soma de não lidas de todas as conversas, compartilhado entre o badge do menu e as telas de chat
export interface ChatUnreadContextValue {
  count: number
  refresh: () => Promise<void>
}

export const ChatUnreadContext = createContext<ChatUnreadContextValue | undefined>(undefined)
