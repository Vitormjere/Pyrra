import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { Send } from 'lucide-react'
import { useChatUnread } from '../hooks/useChatUnread'
import {
  getChatConversation,
  markChatConversationAsRead,
  sendChatMessage,
} from '../services/chatService'
import { getApiErrorMessage } from '../services/apiError'
import type { ChatMessage } from '../types/chat'
import type { UserSummary } from '../types/community'

const inputClasses =
  'w-full rounded-md bg-surface-hi px-3 py-2 text-sm text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green'

/** "14:32" */
function formatTime(iso: string): string {
  return new Date(iso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
}

function Bubble({ message, isMine }: { message: ChatMessage; isMine: boolean }) {
  return (
    <li className={`flex ${isMine ? 'justify-end' : 'justify-start'}`}>
      <div
        className={[
          'max-w-[75%] rounded-2xl px-3 py-2 text-sm',
          isMine
            ? 'rounded-br-sm bg-brand-green text-brand-dark'
            : 'rounded-bl-sm bg-surface-hi text-ink ring-1 ring-line',
        ].join(' ')}
      >
        <p className="whitespace-pre-wrap break-words">{message.content}</p>
        <p className={['mt-0.5 text-[10px]', isMine ? 'text-brand-dark/70' : 'text-slate-500'].join(' ')}>
          {formatTime(message.createdAt)}
        </p>
      </div>
    </li>
  )
}

// Painel de conversa com UMA contraparte — reaproveitado pela tela do admin (Admin/Mensagens) e
// pela do jogador (Suporte). Sem tempo real ainda (Fase Admin-4a): a mensagem enviada aparece na
// hora (é a resposta do próprio POST), mas uma resposta da outra pessoa só aparece ao reabrir/
// trocar de conversa — isso muda só na Fase Admin-4b, sem tocar neste componente por dentro.
//
// Quem usa este componente precisa passar key={counterpart.id} — é isso que reseta o estado
// (mensagens, erro, texto digitado) ao trocar de conversa, em vez de um efeito fazendo o reset na
// mão a cada mudança de prop.
export function ChatPanel({ counterpart }: { counterpart: UserSummary }) {
  const { refresh: refreshUnread } = useChatUnread()

  const [messages, setMessages] = useState<ChatMessage[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [content, setContent] = useState('')
  const [sending, setSending] = useState(false)
  const listEndRef = useRef<HTMLLIElement>(null)

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const data = await getChatConversation(counterpart.id)
        if (!active) return
        setMessages(data)
        if (data.some((m) => m.readAt === null && m.sender.id === counterpart.id)) {
          await markChatConversationAsRead(counterpart.id)
          if (active) await refreshUnread()
        }
      } catch (err) {
        if (!active) return
        setError(getApiErrorMessage(err, {}, 'Não foi possível carregar a conversa.'))
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [counterpart.id, refreshUnread])

  useEffect(() => {
    listEndRef.current?.scrollIntoView({ block: 'end' })
  }, [messages])

  async function handleSend(event: FormEvent) {
    event.preventDefault()
    const trimmed = content.trim()
    if (!trimmed) return

    setSending(true)
    setError(null)
    try {
      const message = await sendChatMessage(counterpart.id, trimmed)
      setMessages((current) => [...(current ?? []), message])
      setContent('')
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível enviar a mensagem.'))
    } finally {
      setSending(false)
    }
  }

  return (
    <div className="flex h-full min-h-0 flex-col">
      <div className="flex items-center gap-3 border-b border-line px-4 py-3">
        <span
          aria-hidden="true"
          className="flex size-9 shrink-0 items-center justify-center rounded-full bg-surface-hi text-sm font-semibold text-slate-300 ring-1 ring-line"
        >
          {counterpart.name.charAt(0).toUpperCase()}
        </span>
        <div className="min-w-0">
          <p className="truncate font-medium text-ink">{counterpart.name}</p>
          {counterpart.username && <p className="truncate text-xs text-slate-500">@{counterpart.username}</p>}
        </div>
      </div>

      <div className="flex-1 overflow-y-auto px-4 py-3">
        {messages === null ? (
          <p className="text-center text-sm text-slate-500">Carregando…</p>
        ) : messages.length === 0 ? (
          <p className="text-center text-sm text-slate-500">Nenhuma mensagem ainda — envie a primeira.</p>
        ) : (
          <ul className="flex flex-col gap-2">
            {messages.map((message) => (
              <Bubble key={message.id} message={message} isMine={message.sender.id !== counterpart.id} />
            ))}
            <li ref={listEndRef} aria-hidden="true" />
          </ul>
        )}
      </div>

      {error && (
        <p role="alert" className="px-4 pb-1 text-xs text-red-300">
          {error}
        </p>
      )}

      <form onSubmit={handleSend} className="flex items-center gap-2 border-t border-line px-4 py-3">
        <input
          type="text"
          value={content}
          onChange={(event) => setContent(event.target.value)}
          placeholder="Escreva uma mensagem…"
          aria-label={`Mensagem para ${counterpart.name}`}
          className={inputClasses}
        />
        <button
          type="submit"
          disabled={sending || !content.trim()}
          aria-label="Enviar mensagem"
          className="inline-flex shrink-0 items-center justify-center rounded-xl bg-brand-green p-2.5 text-brand-dark transition hover:brightness-95 disabled:opacity-60"
        >
          <Send size={16} aria-hidden="true" />
        </button>
      </form>
    </div>
  )
}

export default ChatPanel
