import { useEffect, useState } from 'react'
import { Link, Navigate, useParams } from 'react-router-dom'
import { Trophy } from 'lucide-react'
import { useAuth } from '../../hooks/useAuth'
import Skeleton from '../../components/Skeleton'
import TeamEntryPicker from '../../components/TeamEntryPicker'
import { getMyEligibleTeamsForTournament } from '../../services/teamService'
import { requestTeamEntryViaInvite } from '../../services/tournamentService'
import { getApiErrorMessage } from '../../services/apiError'
import type { Team } from '../../types/teams'

// diferente do convite de time (que entra na hora), aqui só o dono de um time solicita entrada, que fica pendente até o dono do torneio aprovar
export function ConviteTorneio() {
  const { token } = useParams<{ token: string }>()
  const { user, loading } = useAuth()

  const [teams, setTeams] = useState<Team[] | null>(null)
  const [teamsError, setTeamsError] = useState<string | null>(null)

  useEffect(() => {
    if (loading || !user) return
    let active = true

    void getMyEligibleTeamsForTournament()
      .then((data) => {
        if (!active) return
        setTeams(data)
      })
      .catch((err) => {
        if (!active) return
        setTeamsError(getApiErrorMessage(err, {}, 'Não foi possível carregar seus times.'))
      })

    return () => {
      active = false
    }
  }, [loading, user])

  if (loading) {
    return <Centered>Carregando…</Centered>
  }

  // sem o "guarda e retoma depois do login": esse link circula entre donos de time que já têm conta
  if (!user) {
    return <Navigate to="/login" replace />
  }

  return (
    <main className="flex min-h-screen flex-col items-center justify-center px-4 py-12">
      <div className="w-full max-w-sm">
        <div className="text-center">
          <span className="mx-auto mb-5 flex size-14 items-center justify-center rounded-full bg-brand-green/10 text-brand-green">
            <Trophy size={26} />
          </span>
          <h1 className="font-display text-2xl font-semibold tracking-tight text-ink">
            Convite de Torneio
          </h1>
          <p className="mt-2 text-sm text-slate-400">
            Escolha um time seu para solicitar entrada. O dono do torneio precisa aprovar antes de
            valer.
          </p>
        </div>

        <div className="mt-6 flex flex-col gap-2">
          {teamsError && (
            <p role="alert" className="text-center text-xs text-red-300">
              {teamsError}
            </p>
          )}

          {teams === null ? (
            <Skeleton className="h-16" />
          ) : teams.length === 0 ? (
            <p className="rounded-md bg-surface px-4 py-3 text-center text-sm text-slate-500 ring-1 ring-line">
              Nenhum time seu disponível — crie um time, ou aguarde um que já está em outro torneio
              ficar livre.
            </p>
          ) : (
            <TeamEntryPicker
              teams={teams}
              onRequest={(teamId) => requestTeamEntryViaInvite(token!, teamId)}
            />
          )}
        </div>

        <Link
          to="/torneios"
          className="mt-8 inline-block w-full rounded-xl bg-brand-green px-4 py-3 text-center font-semibold text-brand-dark transition hover:brightness-95"
        >
          Ir para Torneios
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

export default ConviteTorneio
