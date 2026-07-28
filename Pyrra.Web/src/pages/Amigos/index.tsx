import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  Check,
  Copy,
  Crown,
  Flame,
  Link2,
  Search,
  Trophy,
  UserMinus,
  UserPlus,
  Users,
  X,
} from 'lucide-react'
import EmptyState from '../../components/EmptyState'
import Skeleton from '../../components/Skeleton'
import { useConfirm } from '../../hooks/useConfirm'
import { useFriendRequests } from '../../hooks/useFriendRequests'
import {
  acceptFriendRequest,
  declineFriendRequest,
  getFriends,
  getInviteLink,
  getPendingRequests,
  getRanking,
  removeFriendship,
  searchUsers,
  sendFriendRequest,
} from '../../services/friendService'
import { getApiErrorMessage } from '../../services/apiError'
import type {
  Friend,
  FriendRequest,
  RankingEntry,
  UserSearchResult,
  UserSummary,
} from '../../types/community'

type Tab = 'amigos' | 'ranking' | 'pedidos' | 'buscar'

// Avatar por inicial — não há foto no modelo; a inicial num círculo é o mesmo recurso do rodapé
// de conta do menu.
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

// Linha de usuário reutilizada nas três abas: avatar + nome + @username, e a ação à direita.
// `to` (só na aba Meus Amigos) torna o bloco avatar+nome um link para o perfil público — fica
// FORA de `trailing` (não envolve o botão de remover) porque um link não pode aninhar outro
// elemento interativo dentro dele.
function UserRow({
  user,
  trailing,
  to,
}: {
  user: UserSummary
  trailing: React.ReactNode
  to?: string
}) {
  const identity = (
    <>
      <Avatar name={user.name} />
      <div className="min-w-0 flex-1">
        <p className="truncate font-medium text-ink">{user.name}</p>
        {user.username && (
          <p className="truncate text-xs text-slate-500">@{user.username}</p>
        )}
      </div>
    </>
  )

  return (
    <li className="flex items-center gap-3 px-4 py-3">
      {to ? (
        <Link to={to} className="flex min-w-0 flex-1 items-center gap-3 rounded-lg -m-1 p-1 transition hover:bg-surface-hi">
          {identity}
        </Link>
      ) : (
        <div className="flex min-w-0 flex-1 items-center gap-3">{identity}</div>
      )}
      {trailing}
    </li>
  )
}

// Linha do ranking: posição (coroa no #1), avatar, nome/@username e o streak atual. A linha do
// próprio usuário ganha um fundo diferenciado para ele se achar rápido na lista, já que ela pode
// aparecer em qualquer posição.
function RankingRow({ entry }: { entry: RankingEntry }) {
  const isTop = entry.position === 1

  return (
    <li
      className={[
        'flex items-center gap-3 px-4 py-3',
        entry.isSelf ? 'bg-brand-green/10 ring-1 ring-inset ring-brand-green/30' : '',
      ].join(' ')}
    >
      <span
        aria-hidden="true"
        className={[
          'flex size-7 shrink-0 items-center justify-center rounded-full text-xs font-semibold tabular-nums',
          isTop ? 'bg-brand-green/15 text-brand-green' : 'text-slate-500',
        ].join(' ')}
      >
        {isTop ? <Crown size={14} /> : `#${entry.position}`}
      </span>
      <Avatar name={entry.user.name} />
      <div className="min-w-0 flex-1">
        <p className="truncate font-medium text-ink">
          {entry.user.name}
          {entry.isSelf && (
            <span className="ml-1.5 text-xs font-normal text-brand-green">(você)</span>
          )}
        </p>
        {entry.user.username && (
          <p className="truncate text-xs text-slate-500">@{entry.user.username}</p>
        )}
      </div>
      <span className="flex shrink-0 items-center gap-1 text-sm font-semibold text-ink tabular-nums">
        <Flame size={14} className="text-brand-green" aria-hidden="true" />
        {entry.currentStreak}
      </span>
    </li>
  )
}

const listClasses =
  'divide-y divide-line overflow-hidden rounded-md bg-surface ring-1 ring-line'

export function Amigos() {
  const { count, refresh: refreshCount } = useFriendRequests()
  const { confirm, dialog } = useConfirm()

  const [tab, setTab] = useState<Tab>('amigos')
  const [error, setError] = useState<string | null>(null)

  const [friends, setFriends] = useState<Friend[]>([])
  const [pending, setPending] = useState<FriendRequest[]>([])
  const [loading, setLoading] = useState(true)

  const [ranking, setRanking] = useState<RankingEntry[]>([])
  const [rankingLoading, setRankingLoading] = useState(true)

  const [busyId, setBusyId] = useState<string | null>(null)

  // Busca
  const [term, setTerm] = useState('')
  const [results, setResults] = useState<UserSearchResult[]>([])
  const [searching, setSearching] = useState(false)

  // Convite
  const [inviteUrl, setInviteUrl] = useState<string | null>(null)
  const [copied, setCopied] = useState(false)

  // Busca pura, sem tocar em estado: usada tanto pela carga inicial (que faz o próprio setState
  // dentro de uma função local, para o setState ficar sempre depois de um await — como a regra
  // react-hooks/set-state-in-effect exige) quanto para recarregar após aceitar um pedido.
  const fetchLists = useCallback(
    () => Promise.all([getFriends(), getPendingRequests()]),
    [],
  )

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const [f, p] = await fetchLists()
        if (!active) return
        setFriends(f)
        setPending(p)
      } catch (err) {
        if (!active) return
        setError(getApiErrorMessage(err, {}, 'Não foi possível carregar seus amigos.'))
      } finally {
        if (active) setLoading(false)
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [fetchLists])

  // Recarrega as duas listas (usado após aceitar um pedido) sem duplicar a lógica de fetch.
  const loadLists = useCallback(async () => {
    try {
      const [f, p] = await fetchLists()
      setFriends(f)
      setPending(p)
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível recarregar seus amigos.'))
    }
  }, [fetchLists])

  // Recarrega o ranking (usado após aceitar um pedido ou remover um amigo) sem duplicar a lógica
  // de fetch usada na carga inicial logo abaixo.
  const loadRanking = useCallback(async () => {
    try {
      const data = await getRanking()
      setRanking(data)
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível carregar o ranking.'))
    } finally {
      setRankingLoading(false)
    }
  }, [])

  // Efeito próprio (não reaproveita loadRanking): um problema no ranking não deve impedir o resto
  // da tela de carregar, e o setState precisa ficar depois de um await dentro do efeito, não
  // escondido atrás de uma função só referenciada por nome (regra react-hooks/set-state-in-effect).
  useEffect(() => {
    let active = true

    async function run() {
      try {
        const data = await getRanking()
        if (!active) return
        setRanking(data)
      } catch (err) {
        if (!active) return
        setError(getApiErrorMessage(err, {}, 'Não foi possível carregar o ranking.'))
      } finally {
        if (active) setRankingLoading(false)
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [])

  // Link de convite: montado com a origem do próprio front, então vale em dev e produção.
  useEffect(() => {
    let active = true
    void (async () => {
      try {
        const link = await getInviteLink()
        if (active) setInviteUrl(`${window.location.origin}${link.path}`)
      } catch {
        // Silencioso: o resto da tela funciona sem o link.
      }
    })()
    return () => {
      active = false
    }
  }, [])

  // Busca com debounce. Termo vazio não zera o estado aqui — a renderização abaixo já ignora
  // resultados/status quando o campo está vazio, então não há necessidade de um setState síncrono
  // só para "limpar", o que a regra react-hooks/set-state-in-effect não permite de qualquer forma.
  useEffect(() => {
    const trimmed = term.trim()
    if (trimmed.length === 0) {
      return
    }

    let active = true
    const timer = setTimeout(async () => {
      // Dentro do callback do timer (função aninhada, não o corpo do efeito): o "buscando"
      // só aparece quando a busca de fato começa, depois do debounce.
      setSearching(true)
      try {
        const found = await searchUsers(trimmed)
        if (active) setResults(found)
      } catch {
        if (active) setResults([])
      } finally {
        if (active) setSearching(false)
      }
    }, 400)

    return () => {
      active = false
      clearTimeout(timer)
    }
  }, [term])

  async function handleCopy() {
    if (!inviteUrl) return
    try {
      await navigator.clipboard.writeText(inviteUrl)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch {
      setError('Não foi possível copiar o link.')
    }
  }

  async function handleAccept(friendshipId: string) {
    setBusyId(friendshipId)
    setError(null)
    try {
      await acceptFriendRequest(friendshipId)
      setPending((current) => current.filter((p) => p.friendshipId !== friendshipId))
      await Promise.all([loadLists(), refreshCount(), loadRanking()])
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível aceitar o pedido.'))
    } finally {
      setBusyId(null)
    }
  }

  async function handleDecline(friendshipId: string) {
    setBusyId(friendshipId)
    setError(null)
    try {
      await declineFriendRequest(friendshipId)
      setPending((current) => current.filter((p) => p.friendshipId !== friendshipId))
      await refreshCount()
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível recusar o pedido.'))
    } finally {
      setBusyId(null)
    }
  }

  async function handleRemove(friend: Friend) {
    const ok = await confirm({
      title: 'Desfazer amizade',
      message: `Remover ${friend.user.name} dos seus amigos?`,
      confirmLabel: 'Remover',
      destructive: true,
    })
    if (!ok) return

    setBusyId(friend.friendshipId)
    setError(null)
    try {
      await removeFriendship(friend.friendshipId)
      setFriends((current) =>
        current.filter((f) => f.friendshipId !== friend.friendshipId),
      )
      await loadRanking()
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível remover a amizade.'))
    } finally {
      setBusyId(null)
    }
  }

  async function handleSend(userId: string) {
    setBusyId(userId)
    setError(null)
    try {
      await sendFriendRequest(userId)
      // Reflete o novo estado no resultado da busca sem refazer a consulta.
      setResults((current) =>
        current.map((r) =>
          r.user.id === userId ? { ...r, state: 'RequestSent' } : r,
        ),
      )
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível enviar o pedido.'))
    } finally {
      setBusyId(null)
    }
  }

  const tabs: { id: Tab; label: string; badge?: number }[] = [
    { id: 'amigos', label: 'Meus Amigos' },
    { id: 'ranking', label: 'Ranking' },
    { id: 'pedidos', label: 'Pedidos', badge: count },
    { id: 'buscar', label: 'Buscar' },
  ]

  return (
    <div className="flex flex-col gap-5">
      <header>
        <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">
          Amigos
        </h1>
      </header>

      {/* LINK DE CONVITE */}
      <section className="flex flex-col gap-2 rounded-md bg-surface px-4 py-3 ring-1 ring-line">
        <div className="flex items-center gap-2 text-sm font-medium text-ink">
          <Link2 size={16} className="text-brand-green" aria-hidden="true" />
          Seu link de convite
        </div>
        <p className="text-xs text-slate-400">
          Quem abrir esse link já te envia um pedido de amizade.
        </p>
        <div className="flex gap-2">
          <input
            type="text"
            readOnly
            value={inviteUrl ?? 'Gerando…'}
            aria-label="Link de convite"
            onFocus={(event) => event.target.select()}
            className="min-w-0 flex-1 rounded-md bg-surface-hi px-3 py-2 text-xs text-slate-300 ring-1 ring-line outline-none"
          />
          <button
            type="button"
            onClick={handleCopy}
            disabled={!inviteUrl}
            className="inline-flex shrink-0 items-center gap-1.5 rounded-xl bg-brand-green px-3 py-2 text-sm font-semibold text-brand-dark transition hover:brightness-95 disabled:opacity-60"
          >
            {copied ? <Check size={15} /> : <Copy size={15} />}
            {copied ? 'Copiado' : 'Copiar'}
          </button>
        </div>
      </section>

      {/* ABAS */}
      <div role="tablist" className="flex gap-1 rounded-md bg-surface p-1">
        {tabs.map((t) => (
          <button
            key={t.id}
            type="button"
            role="tab"
            aria-selected={tab === t.id}
            onClick={() => setTab(t.id)}
            className={[
              'flex flex-1 items-center justify-center gap-1.5 rounded-lg px-3 py-2 text-sm font-medium transition',
              tab === t.id
                ? 'bg-surface-hi text-ink'
                : 'text-slate-400 hover:text-slate-200',
            ].join(' ')}
          >
            {t.label}
            {t.badge !== undefined && t.badge > 0 && (
              <span className="rounded-full bg-brand-green px-1.5 py-0.5 text-[10px] font-semibold text-brand-dark tabular-nums">
                {t.badge}
              </span>
            )}
          </button>
        ))}
      </div>

      {error && (
        <p
          role="alert"
          className="rounded-lg bg-red-500/10 px-3 py-2 text-center text-sm text-red-300 ring-1 ring-red-500/20"
        >
          {error}
        </p>
      )}

      {/* MEUS AMIGOS */}
      {tab === 'amigos' &&
        (loading ? (
          <Skeleton className="h-20" />
        ) : friends.length === 0 ? (
          <EmptyState
            icon={Users}
            title="Você ainda não tem amigos por aqui."
            description="Busque por username ou compartilhe seu link de convite."
          />
        ) : (
          <ul className={listClasses}>
            {friends.map((friend) => (
              <UserRow
                key={friend.friendshipId}
                user={friend.user}
                // Sem username não há como montar a rota /perfil/:username — não deveria
                // acontecer (só quem já escolheu username pode virar amigo), mas a linha
                // continua útil mesmo sem o link nesse caso defensivo.
                to={friend.user.username ? `/perfil/${friend.user.username}` : undefined}
                trailing={
                  <button
                    type="button"
                    disabled={busyId === friend.friendshipId}
                    onClick={() => handleRemove(friend)}
                    aria-label={`Remover ${friend.user.name}`}
                    className="shrink-0 rounded-lg p-2 text-slate-500 transition hover:bg-surface-hi hover:text-red-400 disabled:opacity-50"
                  >
                    <UserMinus size={16} />
                  </button>
                }
              />
            ))}
          </ul>
        ))}

      {/* RANKING */}
      {tab === 'ranking' &&
        (rankingLoading ? (
          <Skeleton className="h-20" />
        ) : ranking.length <= 1 ? (
          <EmptyState
            icon={Trophy}
            title="Adicione amigos para competir no ranking."
            description="Assim que você tiver amigos confirmados, o ranking por streak aparece aqui."
          />
        ) : (
          <ol className={listClasses}>
            {ranking.map((entry) => (
              <RankingRow key={entry.user.id} entry={entry} />
            ))}
          </ol>
        ))}

      {/* PEDIDOS RECEBIDOS */}
      {tab === 'pedidos' &&
        (loading ? (
          <Skeleton className="h-20" />
        ) : pending.length === 0 ? (
          <EmptyState
            icon={UserPlus}
            title="Nenhum pedido pendente."
            description="Pedidos de amizade que você receber aparecem aqui."
          />
        ) : (
          <ul className={listClasses}>
            {pending.map((request) => (
              <UserRow
                key={request.friendshipId}
                user={request.requester}
                trailing={
                  <div className="flex shrink-0 gap-1.5">
                    <button
                      type="button"
                      disabled={busyId === request.friendshipId}
                      onClick={() => handleAccept(request.friendshipId)}
                      className="inline-flex items-center gap-1 rounded-xl bg-brand-green px-3 py-1.5 text-sm font-semibold text-brand-dark transition hover:brightness-95 disabled:opacity-60"
                    >
                      <Check size={15} />
                      Aceitar
                    </button>
                    <button
                      type="button"
                      disabled={busyId === request.friendshipId}
                      onClick={() => handleDecline(request.friendshipId)}
                      aria-label={`Recusar pedido de ${request.requester.name}`}
                      className="rounded-xl p-2 text-slate-400 ring-1 ring-line transition hover:bg-surface-hi hover:text-ink disabled:opacity-50"
                    >
                      <X size={15} />
                    </button>
                  </div>
                }
              />
            ))}
          </ul>
        ))}

      {/* BUSCAR */}
      {tab === 'buscar' && (
        <div className="flex flex-col gap-3">
          <div className="relative">
            <Search
              size={16}
              aria-hidden="true"
              className="absolute top-1/2 left-3 -translate-y-1/2 text-slate-500"
            />
            <input
              type="text"
              value={term}
              onChange={(event) => setTerm(event.target.value)}
              placeholder="Buscar por username ou email"
              aria-label="Buscar usuários"
              autoCapitalize="none"
              spellCheck={false}
              className="w-full rounded-md bg-surface px-4 py-3 pl-9 text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green"
            />
          </div>

          {term.trim().length > 0 && searching && (
            <p className="text-center text-sm text-slate-500">Buscando…</p>
          )}

          {term.trim().length > 0 && !searching && results.length === 0 && (
            <p className="text-center text-sm text-slate-500">
              Ninguém encontrado para “{term.trim()}”.
            </p>
          )}

          {/* Guardado por term (não só por results.length): sem isso, limpar o campo enquanto uma
              busca anterior ainda não zerou o estado mostraria resultados de um termo que já
              não está mais no campo. */}
          {term.trim().length > 0 && results.length > 0 && (
            <ul className={listClasses}>
              {results.map((result) => (
                <UserRow
                  key={result.user.id}
                  user={result.user}
                  trailing={
                    <SearchAction
                      state={result.state}
                      busy={busyId === result.user.id}
                      onAdd={() => handleSend(result.user.id)}
                    />
                  }
                />
              ))}
            </ul>
          )}
        </div>
      )}

      {dialog}
    </div>
  )
}

// Botão/rótulo do resultado da busca conforme o vínculo já existente.
function SearchAction({
  state,
  busy,
  onAdd,
}: {
  state: UserSearchResult['state']
  busy: boolean
  onAdd: () => void
}) {
  if (state === 'Friends') {
    return <span className="shrink-0 text-xs font-medium text-brand-green">Amigos</span>
  }
  if (state === 'RequestSent') {
    return <span className="shrink-0 text-xs font-medium text-slate-500">Pendente</span>
  }
  if (state === 'RequestReceived') {
    return (
      <span className="shrink-0 text-xs font-medium text-slate-400">Te enviou pedido</span>
    )
  }
  return (
    <button
      type="button"
      disabled={busy}
      onClick={onAdd}
      className="inline-flex shrink-0 items-center gap-1 rounded-xl bg-brand-green px-3 py-1.5 text-sm font-semibold text-brand-dark transition hover:brightness-95 disabled:opacity-60"
    >
      <UserPlus size={15} />
      Adicionar
    </button>
  )
}

export default Amigos
