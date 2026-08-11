import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { MessageCircle, Send } from 'lucide-react'
import Avatar from './Avatar'
import { useAuth } from '../hooks/useAuth'
import { useChatConnection } from '../hooks/useChatConnection'
import { getTeamChatMessages, sendTeamChatMessage } from '../services/teamChatService'
import { getApiErrorMessage } from '../services/apiError'
import type { TeamChatMessage } from '../types/teamChat'

const inputClasses =
  'w-full rounded-md bg-surface-hi px-3 py-2 text-sm text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green'

// formata tipo "14:32"
function formatTime(iso: string): string {
  return new Date(iso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
}

// bolha de grupo — ao contrário do chat 1-a-1, sempre mostra quem mandou (nome/@usuário),
// porque com vários remetentes o alinhamento esquerda/direita sozinho não basta
function Bubble({ message, isMine }: { message: TeamChatMessage; isMine: boolean }) {
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
        <div className="flex items-center gap-1.5">
          <Avatar name={message.sender.name} imageUrl={message.sender.profilePictureUrl} size="xs" />
          <p
            className={[
              'text-[11px] font-semibold',
              isMine ? 'text-brand-dark/70' : 'text-brand-green',
            ].join(' ')}
          >
            {isMine ? 'Você' : message.sender.username ? `@${message.sender.username}` : message.sender.name}
          </p>
        </div>
        <p className="whitespace-pre-wrap break-words">{message.content}</p>
        <p className={['mt-0.5 text-[10px]', isMine ? 'text-brand-dark/70' : 'text-slate-500'].join(' ')}>
          {formatTime(message.createdAt)}
        </p>
      </div>
    </li>
  )
}

// Chat em grupo do time — visível só pra dono e membros. Mesmo padrão de tempo real do
// ChatPanel (REST pra enviar/carregar histórico + push via hub pra chegada ao vivo), sem
// contagem de não lidas: aqui o requisito é só ver quem mandou e quando, não um badge.
export function TeamChatPanel({ teamId }: { teamId: string }) {
  const { user } = useAuth()
  const connection = useChatConnection()

  const [messages, setMessages] = useState<TeamChatMessage[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [content, setContent] = useState('')
  const [sending, setSending] = useState(false)
  const listEndRef = useRef<HTMLLIElement>(null)

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const data = await getTeamChatMessages(teamId)
        if (active) setMessages(data)
      } catch (err) {
        if (!active) return
        setError(getApiErrorMessage(err, {}, 'Não foi possível carregar a conversa do time.'))
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [teamId])

  useEffect(() => {
    listEndRef.current?.scrollIntoView({ block: 'end' })
  }, [messages])

  // mensagem de outro membro chega ao vivo via hub — filtra pelo time atual, a própria mensagem
  // enviada por mim já entrou na lista pela resposta do POST
  useEffect(() => {
    if (!connection) return

    function handleReceiveTeamMessage(message: TeamChatMessage) {
      if (message.teamId !== teamId || message.sender.id === user?.id) return
      setMessages((current) => [...(current ?? []), message])
    }

    connection.on('ReceiveTeamMessage', handleReceiveTeamMessage)

    return () => {
      connection.off('ReceiveTeamMessage', handleReceiveTeamMessage)
    }
  }, [connection, teamId, user?.id])

  async function handleSend(event: FormEvent) {
    event.preventDefault()
    const trimmed = content.trim()
    if (!trimmed) return

    setSending(true)
    setError(null)
    try {
      const message = await sendTeamChatMessage(teamId, trimmed)
      setMessages((current) => [...(current ?? []), message])
      setContent('')
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível enviar a mensagem.'))
    } finally {
      setSending(false)
    }
  }

  return (
    <section className="flex flex-col gap-2">
      <h2 className="flex items-center gap-1.5 text-sm font-medium text-slate-300">
        <MessageCircle size={15} className="text-brand-green" aria-hidden="true" />
        Chat do time
      </h2>

      <div className="flex flex-col overflow-hidden rounded-md bg-surface ring-1 ring-line">
        <div className="flex max-h-80 flex-col gap-2 overflow-y-auto px-4 py-3">
          {messages === null ? (
            <p className="text-center text-sm text-slate-500">Carregando…</p>
          ) : messages.length === 0 ? (
            <p className="text-center text-sm text-slate-500">Nenhuma mensagem ainda, envie a primeira pro time.</p>
          ) : (
            <ul className="flex flex-col gap-2">
              {messages.map((message) => (
                <Bubble key={message.id} message={message} isMine={message.sender.id === user?.id} />
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
            placeholder="Escreva uma mensagem pro time…"
            aria-label="Mensagem para o time"
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
    </section>
  )
}

export default TeamChatPanel
