import { useEffect, useState } from 'react'
import { ChevronLeft, LifeBuoy } from 'lucide-react'
import Avatar from '../../components/Avatar'
import ChatPanel from '../../components/ChatPanel'
import EmptyState from '../../components/EmptyState'
import Skeleton from '../../components/Skeleton'
import { getActiveAdmins, getChatConversations } from '../../services/chatService'
import { getApiErrorMessage } from '../../services/apiError'
import type { ChatConversation } from '../../types/chat'
import type { UserSummary } from '../../types/community'

const listClasses = 'divide-y divide-line overflow-hidden rounded-md bg-surface ring-1 ring-line'

// chat do jogador com o(s) admin(s) — com um admin só vai direto pra conversa, com mais de um mostra uma lista pra escolher
export function Suporte() {
  const [admins, setAdmins] = useState<UserSummary[] | null>(null)
  const [conversations, setConversations] = useState<ChatConversation[] | null>(null)
  const [selected, setSelected] = useState<UserSummary | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const [adminList, conversationList] = await Promise.all([getActiveAdmins(), getChatConversations()])
        if (!active) return
        setAdmins(adminList)
        setConversations(conversationList)
        if (adminList.length === 1) {
          setSelected(adminList[0])
        }
      } catch (err) {
        if (!active) return
        setError(getApiErrorMessage(err, {}, 'Não foi possível carregar o suporte.'))
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [])

  return (
    <div className="flex flex-col gap-5">
      <header className="flex items-center gap-2">
        {/* Voltar pra lista só faz sentido com mais de um admin — com um só, não há lista. */}
        {selected && admins && admins.length > 1 ? (
          <button
            type="button"
            onClick={() => setSelected(null)}
            aria-label="Voltar para a lista de admins"
            className="-ml-2 rounded-lg p-2 text-slate-400 transition hover:bg-surface hover:text-ink"
          >
            <ChevronLeft size={22} />
          </button>
        ) : null}
        <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">
          Suporte
        </h1>
      </header>

      {error && (
        <p
          role="alert"
          className="rounded-lg bg-red-500/10 px-3 py-2 text-center text-sm text-red-300 ring-1 ring-red-500/20"
        >
          {error}
        </p>
      )}

      {admins === null ? (
        <Skeleton className="h-64" />
      ) : admins.length === 0 ? (
        <EmptyState
          icon={LifeBuoy}
          title="Nenhum admin disponível no momento."
          description="Tente novamente mais tarde."
        />
      ) : selected ? (
        <div className="h-[70vh] overflow-hidden rounded-md bg-surface ring-1 ring-line">
          <ChatPanel key={selected.id} counterpart={selected} />
        </div>
      ) : (
        <ul className={listClasses}>
          {admins.map((admin) => {
            const conversation = conversations?.find((c) => c.counterpart.id === admin.id)
            return (
              <li key={admin.id}>
                <button
                  type="button"
                  onClick={() => setSelected(admin)}
                  className="flex w-full items-center gap-3 px-4 py-3 text-left transition hover:bg-surface-hi"
                >
                  <Avatar name={admin.name} imageUrl={admin.profilePictureUrl} />
                  <div className="min-w-0 flex-1">
                    <p className="truncate font-medium text-ink">{admin.name}</p>
                    <p className="truncate text-xs text-slate-500">
                      {conversation ? conversation.lastMessageContent : 'Iniciar conversa'}
                    </p>
                  </div>
                  {conversation && conversation.unreadCount > 0 && (
                    <span className="shrink-0 rounded-full bg-brand-green px-1.5 py-0.5 text-[10px] font-semibold text-brand-dark tabular-nums">
                      {conversation.unreadCount}
                    </span>
                  )}
                </button>
              </li>
            )
          })}
        </ul>
      )}
    </div>
  )
}

export default Suporte
