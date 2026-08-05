import { useEffect, useRef, useState } from 'react'
import { Link, Navigate, useParams } from 'react-router-dom'
import { Check, UserPlus } from 'lucide-react'
import { useAuth } from '../../hooks/useAuth'
import { acceptInvite } from '../../services/friendService'
import { PENDING_INVITE_KEY } from '../../contexts/friend-requests-context'
import type { InviteOutcome } from '../../types/community'

type State =
  | { status: 'loading' }
  | { status: 'done'; outcome: InviteOutcome; ownerName: string }
  | { status: 'error' }

// mensagem por desfecho, na perspectiva de quem abriu o link
function messageFor(outcome: InviteOutcome, ownerName: string): string {
  switch (outcome) {
    case 'RequestSent':
      return `Pedido de amizade enviado para ${ownerName}.`
    case 'AlreadyFriends':
      return `Você e ${ownerName} já são amigos.`
    case 'AlreadyPending':
      return `Já existe um pedido de amizade entre você e ${ownerName}.`
    case 'OwnLink':
      return 'Esse é o seu próprio link de convite — compartilhe com seus amigos.'
  }
}

// logado: envia o pedido na hora. deslogado: guarda o token e manda pro login — o provider consome depois
export function Convite() {
  const { token } = useParams<{ token: string }>()
  const { user, loading } = useAuth()
  const [state, setState] = useState<State>({ status: 'loading' })

  // evita disparar o aceite duas vezes (StrictMode remonta o efeito em dev)
  const attempted = useRef(false)

  useEffect(() => {
    if (loading || !user || !token || attempted.current) return
    attempted.current = true

    void (async () => {
      try {
        const result = await acceptInvite(token)
        setState({
          status: 'done',
          outcome: result.outcome,
          ownerName: result.owner.name,
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
    if (token) localStorage.setItem(PENDING_INVITE_KEY, token)
    return <Navigate to="/login" replace />
  }

  return (
    <main className="flex min-h-screen flex-col items-center justify-center px-4 py-12">
      <div className="w-full max-w-sm text-center">
        <span className="mx-auto mb-5 flex size-14 items-center justify-center rounded-full bg-brand-green/10 text-brand-green">
          {state.status === 'done' && state.outcome !== 'RequestSent' ? (
            <Check size={26} />
          ) : (
            <UserPlus size={26} />
          )}
        </span>

        <h1 className="font-display text-2xl font-semibold tracking-tight text-ink">
          Convite do Pyrra
        </h1>

        <p className="mt-2 text-sm text-slate-400">
          {state.status === 'loading' && 'Processando convite…'}
          {state.status === 'error' && 'Convite inválido ou expirado.'}
          {state.status === 'done' && messageFor(state.outcome, state.ownerName)}
        </p>

        <Link
          to="/amigos"
          className="mt-8 inline-block w-full rounded-xl bg-brand-green px-4 py-3 font-semibold text-brand-dark transition hover:brightness-95"
        >
          Ir para Amigos
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

export default Convite
