import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Check, Compass, Crown, Plus, Shield, Users, X } from 'lucide-react'
import EmptyState from '../../components/EmptyState'
import Skeleton from '../../components/Skeleton'
import TeamBanner from '../../components/TeamBanner'
import { useAuth } from '../../hooks/useAuth'
import { useTeamInvites } from '../../hooks/useTeamInvites'
import {
  acceptTeamInvite,
  declineTeamInvite,
  getAllTeams,
  getMyTeams,
  getPendingTeamInvites,
  getPublicTeams,
  joinTeamViaLink,
} from '../../services/teamService'
import { getApiErrorMessage } from '../../services/apiError'
import type { PublicTeam, Team, TeamInvite } from '../../types/teams'

// Avatar por inicial — mesmo recurso usado em Amigos, sem foto no modelo.
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

const listClasses =
  'divide-y divide-line overflow-hidden rounded-md bg-surface ring-1 ring-line'

// Miniatura 4:3 no lugar do avatar por inicial — mesmo componente/proporção do banner de
// Explorar e Detalhes, só menor. Sem `title`: o nome já aparece ao lado, em texto; sobrepor de
// novo dentro de uma caixa de ~64px ficaria ilegível.
function TeamRow({ team }: { team: Team }) {
  return (
    <li className="overflow-hidden rounded-md bg-surface ring-1 ring-line">
      <Link to={`/times/${team.id}`} className="flex items-center gap-3 p-3 transition hover:bg-surface-hi">
        <TeamBanner
          theme={team.bannerTheme}
          imageUrl={team.bannerImageUrl}
          className="w-16 shrink-0 rounded-lg"
        />
        <div className="min-w-0 flex-1">
          <p className="flex items-center gap-1.5 truncate font-medium text-ink">
            {team.name}
            {team.isOwner && <Crown size={13} className="shrink-0 text-brand-green" aria-hidden="true" />}
          </p>
          {team.description && (
            <p className="truncate text-xs text-slate-500">{team.description}</p>
          )}
        </div>
        <span className="shrink-0 text-xs font-medium text-slate-400 tabular-nums">
          {team.memberCount}/{team.memberLimit}
        </span>
      </Link>
    </li>
  )
}

// Linha da listagem administrativa (Fase Admin-2.1) — nome, dono, contagem de membros, sem link
// nem ação: GetDetailsAsync exige dono-ou-membro, e o admin normalmente não é nenhum dos dois
// pra times alheios, então um link pra /times/:id quebraria com 404 na maioria dos casos.
function AdminTeamRow({ team }: { team: Team }) {
  return (
    <li className="flex items-center gap-3 p-3">
      <TeamBanner
        theme={team.bannerTheme}
        imageUrl={team.bannerImageUrl}
        className="w-16 shrink-0 rounded-lg"
      />
      <div className="min-w-0 flex-1">
        <p className="truncate font-medium text-ink">{team.name}</p>
        <p className="truncate text-xs text-slate-500">
          Dono: {team.owner.name}
          {team.owner.username && ` (@${team.owner.username})`}
        </p>
      </div>
      <span className="shrink-0 text-xs font-medium text-slate-400 tabular-nums">
        {team.memberCount}/{team.memberLimit}
      </span>
    </li>
  )
}

function ExploreCard({
  publicTeam,
  busy,
  error,
  onJoin,
}: {
  publicTeam: PublicTeam
  busy: boolean
  error?: string
  onJoin: () => void
}) {
  const { team } = publicTeam

  return (
    <li className="flex flex-col overflow-hidden rounded-md bg-surface ring-1 ring-line">
      <TeamBanner
        theme={team.bannerTheme}
        imageUrl={team.bannerImageUrl}
        title={team.bannerImageUrl ? team.name : undefined}
        className="w-full"
      />
      <div className="flex flex-1 flex-col gap-2 p-4">
        {/* Com imagem o nome já vem sobreposto no banner (title acima) — sem imagem, continua
            aqui embaixo como sempre foi. */}
        {!team.bannerImageUrl && <p className="truncate font-medium text-ink">{team.name}</p>}
        {team.description && (
          <p className="line-clamp-2 text-xs text-slate-500">{team.description}</p>
        )}
        <div className="mt-auto flex items-center justify-between gap-2 pt-1">
          <span className="text-xs font-medium text-slate-400 tabular-nums">
            {team.memberCount}/{team.memberLimit}
          </span>
          {team.isOwner ? (
            <span className="text-xs font-medium text-brand-green">Seu time</span>
          ) : team.isMember ? (
            <span className="text-xs font-medium text-slate-500">Você já é membro</span>
          ) : (
            <button
              type="button"
              disabled={busy}
              onClick={onJoin}
              className="inline-flex shrink-0 items-center rounded-xl bg-brand-green px-3 py-1.5 text-sm font-semibold text-brand-dark transition hover:brightness-95 disabled:opacity-60"
            >
              Entrar
            </button>
          )}
        </div>
        {error && (
          <p role="alert" className="text-xs text-red-300">
            {error}
          </p>
        )}
      </div>
    </li>
  )
}

type Tab = 'meus' | 'explorar'

// Visão de admin (Fase Admin-2.1) — lista única com TODOS os times do site, sem abas
// "Meus Times"/"Explorar" (que não fazem sentido pra quem não joga) e sem ação de entrar.
function AdminTeamsView() {
  const [teams, setTeams] = useState<Team[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const data = await getAllTeams()
        if (!active) return
        setTeams(data)
      } catch (err) {
        if (!active) return
        setError(getApiErrorMessage(err, {}, 'Não foi possível carregar os times.'))
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [])

  return (
    <div className="flex flex-col gap-5">
      <header className="flex items-center justify-between gap-3">
        <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">
          Times
        </h1>
        <Link
          to="/times/novo"
          className="inline-flex shrink-0 items-center gap-1.5 rounded-xl bg-brand-green px-3 py-2 text-sm font-semibold text-brand-dark transition hover:brightness-95"
        >
          <Plus size={15} aria-hidden="true" />
          Criar Time
        </Link>
      </header>

      {error && (
        <p
          role="alert"
          className="rounded-lg bg-red-500/10 px-3 py-2 text-center text-sm text-red-300 ring-1 ring-red-500/20"
        >
          {error}
        </p>
      )}

      {teams === null ? (
        <Skeleton className="h-20" />
      ) : teams.length === 0 ? (
        <EmptyState icon={Shield} title="Nenhum time criado no site ainda." />
      ) : (
        <ul className={listClasses}>
          {teams.map((team) => (
            <AdminTeamRow key={team.id} team={team} />
          ))}
        </ul>
      )}
    </div>
  )
}

function PlayerTeamsView() {
  const { count, refresh: refreshInviteCount } = useTeamInvites()

  const [tab, setTab] = useState<Tab>('meus')
  const [error, setError] = useState<string | null>(null)

  const [teams, setTeams] = useState<Team[]>([])
  const [invites, setInvites] = useState<TeamInvite[]>([])
  const [loading, setLoading] = useState(true)
  const [busyId, setBusyId] = useState<string | null>(null)

  const [publicTeams, setPublicTeams] = useState<PublicTeam[]>([])
  const [explorarLoading, setExplorarLoading] = useState(true)
  const [joinErrors, setJoinErrors] = useState<Record<string, string>>({})
  const [joiningId, setJoiningId] = useState<string | null>(null)

  const fetchLists = useCallback(
    () => Promise.all([getMyTeams(), getPendingTeamInvites()]),
    [],
  )

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const [t, i] = await fetchLists()
        if (!active) return
        setTeams(t)
        setInvites(i)
      } catch (err) {
        if (!active) return
        setError(getApiErrorMessage(err, {}, 'Não foi possível carregar seus times.'))
      } finally {
        if (active) setLoading(false)
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [fetchLists])

  // Efeito próprio (não reaproveita a carga de "meus times"): Explorar é independente, e o
  // setState precisa ficar depois de um await dentro do efeito, não atrás de uma função só
  // referenciada por nome (regra react-hooks/set-state-in-effect).
  useEffect(() => {
    let active = true

    async function run() {
      try {
        const data = await getPublicTeams()
        if (!active) return
        setPublicTeams(data)
      } catch (err) {
        if (!active) return
        setError(getApiErrorMessage(err, {}, 'Não foi possível carregar os times públicos.'))
      } finally {
        if (active) setExplorarLoading(false)
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [])

  const loadLists = useCallback(async () => {
    try {
      const [t, i] = await fetchLists()
      setTeams(t)
      setInvites(i)
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível recarregar seus times.'))
    }
  }, [fetchLists])

  async function handleAccept(inviteId: string) {
    setBusyId(inviteId)
    setError(null)
    try {
      await acceptTeamInvite(inviteId)
      setInvites((current) => current.filter((i) => i.inviteId !== inviteId))
      await Promise.all([loadLists(), refreshInviteCount()])
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível aceitar o convite.'))
    } finally {
      setBusyId(null)
    }
  }

  async function handleDecline(inviteId: string) {
    setBusyId(inviteId)
    setError(null)
    try {
      await declineTeamInvite(inviteId)
      setInvites((current) => current.filter((i) => i.inviteId !== inviteId))
      await refreshInviteCount()
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível recusar o convite.'))
    } finally {
      setBusyId(null)
    }
  }

  async function handleJoin(publicTeam: PublicTeam) {
    const teamId = publicTeam.team.id
    setJoiningId(teamId)
    setJoinErrors((current) => {
      const next = { ...current }
      delete next[teamId]
      return next
    })

    try {
      const result = await joinTeamViaLink(publicTeam.inviteToken)
      if (result.outcome === 'TeamFull') {
        setJoinErrors((current) => ({ ...current, [teamId]: 'Esse time já está cheio.' }))
        return
      }
      // Joined (ou AlreadyMember/OwnLink, que a UI já evita mostrar botão pra esses casos):
      // recarrega Explorar e Meus Times pra refletir a nova contagem/membership.
      const [myTeams, allPublic] = await Promise.all([getMyTeams(), getPublicTeams()])
      setTeams(myTeams)
      setPublicTeams(allPublic)
    } catch (err) {
      setJoinErrors((current) => ({
        ...current,
        [teamId]: getApiErrorMessage(err, {}, 'Não foi possível entrar nesse time.'),
      }))
    } finally {
      setJoiningId(null)
    }
  }

  const tabs: { id: Tab; label: string; badge?: number }[] = [
    { id: 'meus', label: 'Meus Times' },
    { id: 'explorar', label: 'Explorar' },
  ]

  return (
    <div className="flex flex-col gap-5">
      <header className="flex items-center justify-between gap-3">
        <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">
          Times
        </h1>
        <Link
          to="/times/novo"
          className="inline-flex shrink-0 items-center gap-1.5 rounded-xl bg-brand-green px-3 py-2 text-sm font-semibold text-brand-dark transition hover:brightness-95"
        >
          <Plus size={15} aria-hidden="true" />
          Criar Time
        </Link>
      </header>

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

      {/* MEUS TIMES */}
      {tab === 'meus' && (
        <>
          {/* CONVITES RECEBIDOS — só aparece quando há algo pendente. */}
          {!loading && invites.length > 0 && (
            <section className="flex flex-col gap-2">
              <h2 className="flex items-center gap-1.5 text-sm font-medium text-slate-300">
                Convites recebidos
                {count > 0 && (
                  <span className="rounded-full bg-brand-green px-1.5 py-0.5 text-[10px] font-semibold text-brand-dark tabular-nums">
                    {count}
                  </span>
                )}
              </h2>
              <ul className={listClasses}>
                {invites.map((invite) => (
                  <li key={invite.inviteId} className="flex items-center gap-3 px-4 py-3">
                    <Avatar name={invite.team.name} />
                    <div className="min-w-0 flex-1">
                      <p className="truncate font-medium text-ink">{invite.team.name}</p>
                      <p className="truncate text-xs text-slate-500">
                        Convite de {invite.inviter.name}
                      </p>
                    </div>
                    <div className="flex shrink-0 gap-1.5">
                      <button
                        type="button"
                        disabled={busyId === invite.inviteId}
                        onClick={() => handleAccept(invite.inviteId)}
                        className="inline-flex items-center gap-1 rounded-xl bg-brand-green px-3 py-1.5 text-sm font-semibold text-brand-dark transition hover:brightness-95 disabled:opacity-60"
                      >
                        <Check size={15} />
                        Aceitar
                      </button>
                      <button
                        type="button"
                        disabled={busyId === invite.inviteId}
                        onClick={() => handleDecline(invite.inviteId)}
                        aria-label={`Recusar convite de ${invite.team.name}`}
                        className="rounded-xl p-2 text-slate-400 ring-1 ring-line transition hover:bg-surface-hi hover:text-ink disabled:opacity-50"
                      >
                        <X size={15} />
                      </button>
                    </div>
                  </li>
                ))}
              </ul>
            </section>
          )}

          {loading ? (
            <Skeleton className="h-20" />
          ) : teams.length === 0 ? (
            <EmptyState
              icon={Users}
              title="Você ainda não faz parte de nenhum time."
              description="Crie um time, aceite um convite, ou explore os times públicos."
            />
          ) : (
            <ul className="flex flex-col gap-2">
              {teams.map((team) => (
                <TeamRow key={team.id} team={team} />
              ))}
            </ul>
          )}
        </>
      )}

      {/* EXPLORAR */}
      {tab === 'explorar' &&
        (explorarLoading ? (
          <Skeleton className="h-20" />
        ) : publicTeams.length === 0 ? (
          <EmptyState
            icon={Compass}
            title="Nenhum time público no momento."
            description="Times marcados como públicos aparecem aqui pra qualquer um entrar direto."
          />
        ) : (
          <ul className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            {publicTeams.map((publicTeam) => (
              <ExploreCard
                key={publicTeam.team.id}
                publicTeam={publicTeam}
                busy={joiningId === publicTeam.team.id}
                error={joinErrors[publicTeam.team.id]}
                onJoin={() => handleJoin(publicTeam)}
              />
            ))}
          </ul>
        ))}
    </div>
  )
}

export function Times() {
  const { user } = useAuth()
  return user?.isAdmin ? <AdminTeamsView /> : <PlayerTeamsView />
}

export default Times
