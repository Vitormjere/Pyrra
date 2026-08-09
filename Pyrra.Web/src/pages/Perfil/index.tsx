import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Award, Flame, Settings, Trophy, Users } from 'lucide-react'
import SectionHeader from '../../components/SectionHeader'
import AchievementCard from '../../components/AchievementCard'
import EmptyState from '../../components/EmptyState'
import Skeleton from '../../components/Skeleton'
import { useAuth } from '../../hooks/useAuth'
import { getFriendsCount } from '../../services/friendService'
import { getStreakStatus } from '../../services/streakService'
import { getAchievements } from '../../services/achievementService'
import { ACHIEVEMENT_TYPE_LABELS } from '../../utils/achievementDisplay'
import type { CommunicationTone } from '../../types/auth'
import type { AchievementResponse, AchievementType } from '../../types/achievement'

type ProfileTab = 'perfil' | 'conquistas'

const TONE_LABELS: Record<CommunicationTone, string> = {
  Direto: 'Direto',
  Acolhedor: 'Acolhedor',
  Desafiador: 'Desafiador',
}

// identidade, números que valem a pena mostrar e um resumo só leitura das preferências — editar é tudo em /configuracoes, aqui não tem form nenhum
export function Perfil() {
  const { user } = useAuth()

  const [friendCount, setFriendCount] = useState<number | null>(null)
  const [streak, setStreak] = useState<{ current: number; best: number } | null>(null)
  const [tab, setTab] = useState<ProfileTab>('perfil')
  // null = ainda carregando, [] = carregou e não tem nada — diferente de friendCount/streak, a aba Conquistas precisa distinguir os dois pra não piscar "sem conquistas" antes da resposta chegar
  const [achievements, setAchievements] = useState<AchievementResponse[] | null>(null)
  const [achievementsError, setAchievementsError] = useState(false)

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const count = await getFriendsCount()
        if (active) setFriendCount(count)
      } catch {
        // silencioso: um número a menos não deve derrubar a tela
      }

      try {
        const status = await getStreakStatus()
        if (active) setStreak({ current: status.displayCount, best: status.bestCount })
      } catch {
        // idem — o streak é um extra, não o núcleo da tela
      }

      try {
        const list = await getAchievements()
        if (active) setAchievements(list)
      } catch {
        if (active) setAchievementsError(true)
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [])

  if (!user) return null

  const grouped = groupByType(achievements ?? [])

  return (
    <div className="flex flex-col gap-5">
      <header className="flex items-center justify-between">
        <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">
          Perfil
        </h1>
        <Link
          to="/configuracoes"
          aria-label="Configurações"
          className="rounded-lg p-2 text-slate-400 transition hover:bg-surface hover:text-ink"
        >
          <Settings size={22} />
        </Link>
      </header>

      {/* Abas — mesmo padrão de role=tab usado em Hoje (Foco/Treino). */}
      <div role="tablist" className="flex gap-1 rounded-md bg-surface p-1">
        {(
          [
            { key: 'perfil' as const, label: 'Perfil', icon: Settings },
            { key: 'conquistas' as const, label: 'Conquistas', icon: Award },
          ]
        ).map((option) => (
          <button
            key={option.key}
            type="button"
            role="tab"
            aria-selected={tab === option.key}
            onClick={() => setTab(option.key)}
            className={[
              'flex flex-1 items-center justify-center gap-2 rounded-lg px-3 py-2 text-sm font-medium transition',
              tab === option.key ? 'bg-surface-hi text-ink' : 'text-slate-400 hover:text-slate-200',
            ].join(' ')}
          >
            <option.icon size={15} aria-hidden="true" />
            {option.label}
          </button>
        ))}
      </div>

      {tab === 'perfil' && (
      <>
      {/* IDENTIDADE */}
      <section className="flex flex-col items-center gap-3 rounded-md bg-surface px-5 py-6 ring-1 ring-line">
        <span
          aria-hidden="true"
          className="flex size-16 items-center justify-center rounded-full bg-surface-hi text-2xl font-semibold text-ink ring-1 ring-line"
        >
          {user.name.charAt(0).toUpperCase()}
        </span>
        <div className="text-center">
          <p className="text-lg font-semibold text-ink">{user.name}</p>
          {user.username && (
            <p className="mt-0.5 text-sm font-medium text-brand-green">@{user.username}</p>
          )}
        </div>
        <span className="inline-block rounded-full bg-brand-green/10 px-3 py-1 text-xs font-medium text-brand-green">
          Plano {user.plan}
        </span>

        {/* Números — amigos e streak, lado a lado. Cada um aparece só quando carregou, sem
            placeholder de "0" enquanto a chamada está em voo (evita mostrar zero e depois pular
            para o valor real). */}
        {(friendCount !== null || streak !== null) && (
          <div className="mt-1 flex w-full divide-x divide-line border-t border-line pt-3">
            {friendCount !== null && (
              <Link
                to="/amigos"
                className="flex flex-1 flex-col items-center gap-0.5 rounded-lg py-1 transition hover:bg-surface-hi"
              >
                <span className="flex items-center gap-1.5 text-lg font-semibold tabular-nums text-ink">
                  <Users size={16} className="text-brand-green" aria-hidden="true" />
                  {friendCount}
                </span>
                <span className="text-xs text-slate-500">
                  {friendCount === 1 ? 'amigo' : 'amigos'}
                </span>
              </Link>
            )}
            {streak !== null && (
              <div className="flex flex-1 flex-col items-center gap-0.5 py-1">
                <span className="flex items-center gap-1.5 text-lg font-semibold tabular-nums text-ink">
                  <Flame size={16} className="text-brand-green" aria-hidden="true" />
                  {streak.current}
                </span>
                <span className="flex items-center gap-1 text-xs text-slate-500">
                  <Trophy size={11} aria-hidden="true" />
                  recorde {streak.best}
                </span>
              </div>
            )}
          </div>
        )}
      </section>

      {/* PREFERÊNCIAS — só leitura. Editar é em Configurações. */}
      <section className="flex flex-col gap-3 rounded-md bg-surface px-5 py-4 ring-1 ring-line">
        <SectionHeader>Preferências</SectionHeader>

        <dl className="flex flex-col gap-2 text-sm">
          <div className="flex items-center justify-between">
            <dt className="text-slate-400">Tom das mensagens</dt>
            <dd className="font-medium text-ink">{TONE_LABELS[user.communicationTone]}</dd>
          </div>
          <div className="flex items-center justify-between">
            <dt className="text-slate-400">Mensagem noturna</dt>
            <dd className="font-medium text-ink">{user.eveningNotificationTime}</dd>
          </div>
          <div className="flex items-center justify-between">
            <dt className="text-slate-400">Fuso horário</dt>
            <dd className="font-medium text-ink">{user.timezone}</dd>
          </div>
        </dl>

        <Link
          to="/configuracoes"
          className="text-center text-xs font-medium text-brand-green transition hover:brightness-110"
        >
          Editar em Configurações
        </Link>
      </section>
      </>
      )}

      {tab === 'conquistas' && (
        <ConquistasTab
          achievements={achievements}
          error={achievementsError}
          grouped={grouped}
        />
      )}
    </div>
  )
}

// [] após carregar = de fato sem conquistas ainda; null = ainda em voo — os dois merecem UI diferente
function ConquistasTab({
  achievements,
  error,
  grouped,
}: {
  achievements: AchievementResponse[] | null
  error: boolean
  grouped: [AchievementType, AchievementResponse[]][]
}) {
  if (achievements === null && !error) {
    return <Skeleton className="h-40" />
  }

  if (error) {
    return (
      <EmptyState
        icon={Award}
        title="Não foi possível carregar suas conquistas."
        description="Tente novamente mais tarde."
      />
    )
  }

  if (grouped.length === 0) {
    return (
      <EmptyState
        icon={Award}
        title="Você ainda não desbloqueou conquistas."
        description="Bata sequências e complete desafios para desbloquear."
      />
    )
  }

  return (
    <div className="flex flex-col gap-4">
      {grouped.map(([type, items]) => (
        <section key={type} className="flex flex-col gap-2">
          <SectionHeader>{ACHIEVEMENT_TYPE_LABELS[type]}</SectionHeader>
          <div className="grid grid-cols-3 gap-2">
            {items.map((achievement) => (
              <AchievementCard key={achievement.id} achievement={achievement} />
            ))}
          </div>
        </section>
      ))}
    </div>
  )
}

// preserva a ordem em que o backend já manda (Type, depois Milestone) — só agrupa, não reordena
function groupByType(achievements: AchievementResponse[]): [AchievementType, AchievementResponse[]][] {
  const groups: [AchievementType, AchievementResponse[]][] = []

  for (const achievement of achievements) {
    const lastGroup = groups[groups.length - 1]
    if (lastGroup && lastGroup[0] === achievement.type) {
      lastGroup[1].push(achievement)
    } else {
      groups.push([achievement.type, [achievement]])
    }
  }

  return groups
}

export default Perfil
