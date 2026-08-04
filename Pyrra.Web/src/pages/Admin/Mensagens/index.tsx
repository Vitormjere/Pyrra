import { useEffect, useMemo, useState } from 'react'
import { ChevronLeft, MessageSquare, Search } from 'lucide-react'
import ChatPanel from '../../../components/ChatPanel'
import EmptyState from '../../../components/EmptyState'
import Skeleton from '../../../components/Skeleton'
import { getAllUsers } from '../../../services/adminUserService'
import { getChatConversations } from '../../../services/chatService'
import { getApiErrorMessage } from '../../../services/apiError'
import type { ChatConversation } from '../../../types/chat'
import type { UserSummary } from '../../../types/community'

const inputClasses =
  'w-full rounded-md bg-surface-hi px-3 py-2 text-sm text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green'

interface ContactRow {
  player: UserSummary
  conversation: ChatConversation | null
}

function Avatar({ name }: { name: string }) {
  return (
    <span
      aria-hidden="true"
      className="flex size-9 shrink-0 items-center justify-center rounded-full bg-surface-hi text-sm font-semibold text-slate-300 ring-1 ring-line"
    >
      {name.charAt(0).toUpperCase()}
    </span>
  )
}

function ContactListRow({ row, active, onClick }: { row: ContactRow; active: boolean; onClick: () => void }) {
  return (
    <li>
      <button
        type="button"
        onClick={onClick}
        className={[
          'flex w-full items-center gap-3 px-3 py-2.5 text-left transition',
          active ? 'bg-surface-hi' : 'hover:bg-surface-hi',
        ].join(' ')}
      >
        <Avatar name={row.player.name} />
        <div className="min-w-0 flex-1">
          <p className="truncate text-sm font-medium text-ink">{row.player.name}</p>
          <p className="truncate text-xs text-slate-500">
            {row.conversation ? row.conversation.lastMessageContent : 'Iniciar conversa'}
          </p>
        </div>
        {row.conversation && row.conversation.unreadCount > 0 && (
          <span className="shrink-0 rounded-full bg-brand-green px-1.5 py-0.5 text-[10px] font-semibold text-brand-dark tabular-nums">
            {row.conversation.unreadCount}
          </span>
        )}
      </button>
    </li>
  )
}

// Chat do admin com jogadores (Fase Admin-4a) — lista à esquerda (conversas existentes, ou busca
// pra iniciar uma nova), painel de conversa à direita. Sem tempo real ainda: ver ChatPanel.
export function AdminMensagens() {
  const [players, setPlayers] = useState<UserSummary[] | null>(null)
  const [conversations, setConversations] = useState<ChatConversation[] | null>(null)
  const [selected, setSelected] = useState<UserSummary | null>(null)
  const [search, setSearch] = useState('')
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const [allUsers, conversationList] = await Promise.all([getAllUsers(), getChatConversations()])
        if (!active) return
        setPlayers(
          allUsers
            .filter((u) => !u.isAdmin && u.deletedAt === null)
            .map((u) => ({ id: u.id, name: u.name, username: u.username })),
        )
        setConversations(conversationList)
      } catch (err) {
        if (!active) return
        setError(getApiErrorMessage(err, {}, 'Não foi possível carregar as conversas.'))
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [])

  const rows = useMemo<ContactRow[] | null>(() => {
    if (!players || !conversations) return null

    const term = search.trim().toLowerCase()
    if (!term) {
      // Sem busca: só quem já tem conversa, mais recente primeiro (ordem que já vem da API).
      return conversations.map((c) => ({ player: c.counterpart, conversation: c }))
    }

    // Com busca: qualquer jogador que bata o termo, com ou sem histórico — é como se inicia uma
    // conversa nova.
    return players
      .filter(
        (p) =>
          p.name.toLowerCase().includes(term) ||
          (p.username?.toLowerCase().includes(term) ?? false),
      )
      .map((p) => ({ player: p, conversation: conversations.find((c) => c.counterpart.id === p.id) ?? null }))
  }, [players, conversations, search])

  return (
    <div className="flex flex-col gap-5">
      <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">
        Mensagens
      </h1>

      {error && (
        <p
          role="alert"
          className="rounded-lg bg-red-500/10 px-3 py-2 text-center text-sm text-red-300 ring-1 ring-red-500/20"
        >
          {error}
        </p>
      )}

      <div className="flex h-[75vh] min-h-[420px] gap-4">
        {/* LISTA — no mobile, esconde quando uma conversa está selecionada. */}
        <div className={`flex-col gap-2 lg:flex lg:w-72 lg:shrink-0 ${selected ? 'hidden' : 'flex w-full'}`}>
          <div className="relative">
            <Search
              size={16}
              aria-hidden="true"
              className="absolute top-1/2 left-3 -translate-y-1/2 text-slate-500"
            />
            <input
              type="text"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Buscar jogador"
              aria-label="Buscar jogador"
              autoCapitalize="none"
              spellCheck={false}
              className={`${inputClasses} pl-9`}
            />
          </div>

          {rows === null ? (
            <Skeleton className="h-40" />
          ) : rows.length === 0 ? (
            <EmptyState
              icon={MessageSquare}
              title={search.trim() ? 'Nenhum jogador encontrado.' : 'Nenhuma conversa ainda.'}
              description={search.trim() ? undefined : 'Busque um jogador acima para começar.'}
            />
          ) : (
            <ul className="flex-1 divide-y divide-line overflow-y-auto rounded-md bg-surface ring-1 ring-line">
              {rows.map((row) => (
                <ContactListRow
                  key={row.player.id}
                  row={row}
                  active={selected?.id === row.player.id}
                  onClick={() => setSelected(row.player)}
                />
              ))}
            </ul>
          )}
        </div>

        {/* PAINEL — no mobile, só aparece com uma conversa selecionada; no desktop, sempre visível. */}
        <div
          className={`flex-1 overflow-hidden rounded-md bg-surface ring-1 ring-line ${selected ? 'flex' : 'hidden lg:flex'}`}
        >
          {selected ? (
            <div className="flex h-full w-full flex-col">
              <button
                type="button"
                onClick={() => setSelected(null)}
                className="flex items-center gap-1.5 border-b border-line px-3 py-2 text-xs font-medium text-slate-400 transition hover:text-ink lg:hidden"
              >
                <ChevronLeft size={16} aria-hidden="true" />
                Voltar
              </button>
              <div className="min-h-0 flex-1">
                <ChatPanel key={selected.id} counterpart={selected} />
              </div>
            </div>
          ) : (
            <div className="flex h-full w-full items-center justify-center px-4 text-center text-sm text-slate-500">
              Selecione um jogador na lista para ver a conversa.
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

export default AdminMensagens
