import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { isAxiosError } from 'axios'
import { ChevronLeft, Flame, Lock, Trophy, UserX, Users } from 'lucide-react'
import EmptyState from '../../components/EmptyState'
import Skeleton from '../../components/Skeleton'
import { getPublicProfile } from '../../services/userService'
import type { PublicProfile } from '../../types/profile'

type State =
  | { status: 'loading' }
  | { status: 'ready'; profile: PublicProfile }
  | { status: 'private' }
  | { status: 'not-found' }
  | { status: 'error' }

// perfil público de outro usuário — mesmo visual do Perfil próprio, mas só leitura, sem preferências nem controle de edição
export function PerfilPublico() {
  const { username } = useParams<{ username: string }>()
  const [state, setState] = useState<State>({ status: 'loading' })

  useEffect(() => {
    if (!username) return
    let active = true

    async function run() {
      try {
        const profile = await getPublicProfile(username!)
        if (active) setState({ status: 'ready', profile })
      } catch (err) {
        if (!active) return
        if (isAxiosError(err) && err.response?.status === 403) {
          setState({ status: 'private' })
        } else if (isAxiosError(err) && err.response?.status === 404) {
          setState({ status: 'not-found' })
        } else {
          setState({ status: 'error' })
        }
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [username])

  return (
    <div className="flex flex-col gap-5">
      <header className="flex items-center gap-2">
        <Link
          to="/amigos"
          aria-label="Voltar a Amigos"
          className="-ml-2 rounded-lg p-2 text-slate-400 transition hover:bg-surface hover:text-ink"
        >
          <ChevronLeft size={22} />
        </Link>
        <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">
          Perfil
        </h1>
      </header>

      {state.status === 'loading' && <Skeleton className="h-56" />}

      {state.status === 'private' && (
        <EmptyState
          icon={Lock}
          title="Este perfil é privado."
          description="Só amigos confirmados podem vê-lo."
        />
      )}

      {state.status === 'not-found' && (
        <EmptyState icon={UserX} title="Usuário não encontrado." />
      )}

      {state.status === 'error' && (
        <EmptyState
          icon={UserX}
          title="Não foi possível carregar este perfil."
          description="Tente novamente mais tarde."
        />
      )}

      {state.status === 'ready' && <PublicProfileCard profile={state.profile} />}
    </div>
  )
}

function PublicProfileCard({ profile }: { profile: PublicProfile }) {
  return (
    <section className="flex flex-col items-center gap-3 rounded-md bg-surface px-5 py-6 ring-1 ring-line">
      <span
        aria-hidden="true"
        className="flex size-16 items-center justify-center rounded-full bg-surface-hi text-2xl font-semibold text-ink ring-1 ring-line"
      >
        {profile.name.charAt(0).toUpperCase()}
      </span>
      <div className="text-center">
        <p className="text-lg font-semibold text-ink">{profile.name}</p>
        {profile.username && (
          <p className="mt-0.5 text-sm font-medium text-brand-green">@{profile.username}</p>
        )}
      </div>
      <span className="inline-block rounded-full bg-brand-green/10 px-3 py-1 text-xs font-medium text-brand-green">
        Plano {profile.plan}
      </span>

      <div className="mt-1 flex w-full divide-x divide-line border-t border-line pt-3">
        <div className="flex flex-1 flex-col items-center gap-0.5 py-1">
          <span className="flex items-center gap-1.5 text-lg font-semibold tabular-nums text-ink">
            <Users size={16} className="text-brand-green" aria-hidden="true" />
            {profile.friendCount}
          </span>
          <span className="text-xs text-slate-500">
            {profile.friendCount === 1 ? 'amigo' : 'amigos'}
          </span>
        </div>
        <div className="flex flex-1 flex-col items-center gap-0.5 py-1">
          <span className="flex items-center gap-1.5 text-lg font-semibold tabular-nums text-ink">
            <Flame size={16} className="text-brand-green" aria-hidden="true" />
            {profile.streakCurrent}
          </span>
          <span className="flex items-center gap-1 text-xs text-slate-500">
            <Trophy size={11} aria-hidden="true" />
            recorde {profile.streakBest}
          </span>
        </div>
      </div>
    </section>
  )
}

export default PerfilPublico
