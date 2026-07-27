import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Flame, Settings, Trophy, Users } from 'lucide-react'
import SectionHeader from '../../components/SectionHeader'
import { useAuth } from '../../hooks/useAuth'
import { getFriendsCount } from '../../services/friendService'
import { getStreakStatus } from '../../services/streakService'
import type { CommunicationTone } from '../../types/auth'

const TONE_LABELS: Record<CommunicationTone, string> = {
  Direto: 'Direto',
  Acolhedor: 'Acolhedor',
  Desafiador: 'Desafiador',
}

// Tela pública/social: identidade (nome, @username, avatar), números que valem a pena mostrar
// (amigos, streak) e um resumo SÓ LEITURA das preferências — editar qualquer coisa daqui em
// diante é em /configuracoes, que concentra os formulários. Perfil não tem form nenhum.
export function Perfil() {
  const { user } = useAuth()

  const [friendCount, setFriendCount] = useState<number | null>(null)
  const [streak, setStreak] = useState<{ current: number; best: number } | null>(null)

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const count = await getFriendsCount()
        if (active) setFriendCount(count)
      } catch {
        // Silencioso: um número a menos não deve derrubar a tela.
      }

      try {
        const status = await getStreakStatus()
        if (active) setStreak({ current: status.displayCount, best: status.bestCount })
      } catch {
        // Idem — o streak é um extra, não o núcleo da tela.
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [])

  if (!user) return null

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
    </div>
  )
}

export default Perfil
