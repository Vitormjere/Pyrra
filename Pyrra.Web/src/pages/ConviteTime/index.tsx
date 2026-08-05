import { useEffect, useRef, useState } from 'react'
import { Link, Navigate, useParams } from 'react-router-dom'
import { Check, Users } from 'lucide-react'
import { useAuth } from '../../hooks/useAuth'
import { joinTeamViaLink } from '../../services/teamService'
import { PENDING_TEAM_INVITE_KEY } from '../../contexts/team-invites-context'
import type { JoinOutcome } from '../../types/teams'

type State =
  | { status: 'loading' }
  | { status: 'done'; outcome: JoinOutcome; teamName: string; teamId: string }
  | { status: 'error' }

// mensagem por desfecho, na perspectiva de quem abriu o link
function messageFor(outcome: JoinOutcome, teamName: string): string {
  switch (outcome) {
    case 'Joined':
      return `Você entrou em ${teamName}.`
    case 'AlreadyMember':
      return `Você já faz parte de ${teamName}.`
    case 'TeamFull':
      return `${teamName} já está com o limite de membros — não é possível entrar agora.`
    case 'OwnLink':
      return 'Esse é o link de convite do seu próprio time — compartilhe com outras pessoas.'
  }
}

// logado: entra na hora. deslogado: guarda o token e manda pro login — mesmo padrão de Convite/index.tsx
export function ConviteTime() {
  const { token } = useParams<{ token: string }>()
  const { user, loading } = useAuth()
  const [state, setState] = useState<State>({ status: 'loading' })

  // evita disparar a entrada duas vezes (StrictMode remonta o efeito em dev)
  const attempted = useRef(false)

  useEffect(() => {
    if (loading || !user || !token || attempted.current) return
    attempted.current = true

    void (async () => {
      try {
        const result = await joinTeamViaLink(token)
        setState({
          status: 'done',
          outcome: result.outcome,
          teamName: result.team.name,
          teamId: result.team.id,
        })
      } catch {
        setState({ status: 'error' })
      }
    })()
  }, [loading, user, token])

  if (loading) {
    return <Centered>Carregando…</Centered>
  }

  // deslogado: guarda o convite e manda pro login, o provider consome após autenticar
  if (!user) {
    if (token) localStorage.setItem(PENDING_TEAM_INVITE_KEY, token)
    return <Navigate to="/login" replace />
  }

  return (
    <main className="flex min-h-screen flex-col items-center justify-center px-4 py-12">
      <div className="w-full max-w-sm text-center">
        <span className="mx-auto mb-5 flex size-14 items-center justify-center rounded-full bg-brand-green/10 text-brand-green">
          {state.status === 'done' && state.outcome === 'Joined' ? (
            <Check size={26} />
          ) : (
            <Users size={26} />
          )}
        </span>

        <h1 className="font-display text-2xl font-semibold tracking-tight text-ink">
          Convite de Time
        </h1>

        <p className="mt-2 text-sm text-slate-400">
          {state.status === 'loading' && 'Processando convite…'}
          {state.status === 'error' && 'Convite inválido ou expirado.'}
          {state.status === 'done' && messageFor(state.outcome, state.teamName)}
        </p>

        <Link
          to={state.status === 'done' ? `/times/${state.teamId}` : '/times'}
          className="mt-8 inline-block w-full rounded-xl bg-brand-green px-4 py-3 font-semibold text-brand-dark transition hover:brightness-95"
        >
          Ir para Times
        </Link>
      </div>
    </main>
  )
}

function Centered({ children }: { children: React.ReactNode }) {
  return (
    <main className="flex min-h-screen items-center justify-center px-4 text-sm text-slate-400">
      {children}
    </main>
  )
}

export default ConviteTime
