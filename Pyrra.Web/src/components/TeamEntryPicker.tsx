import { useState } from 'react'
import { Check, Crown } from 'lucide-react'
import TeamBanner from './TeamBanner'
import { getApiErrorMessage } from '../services/apiError'
import type { Team } from '../types/teams'

// lista de times com botão "Solicitar" por linha, reaproveitada no convite e nos detalhes do torneio — onRequest decide qual endpoint chamar
export function TeamEntryPicker({
  teams,
  onRequest,
}: {
  teams: Team[]
  onRequest: (teamId: string) => Promise<unknown>
}) {
  const [busyTeamId, setBusyTeamId] = useState<string | null>(null)
  const [sentTeamIds, setSentTeamIds] = useState<string[]>([])
  const [requestErrors, setRequestErrors] = useState<Record<string, string>>({})

  async function handleRequest(teamId: string) {
    setBusyTeamId(teamId)
    setRequestErrors((current) => {
      const next = { ...current }
      delete next[teamId]
      return next
    })

    try {
      await onRequest(teamId)
      setSentTeamIds((current) => [...current, teamId])
    } catch (err) {
      setRequestErrors((current) => ({
        ...current,
        [teamId]: getApiErrorMessage(err, {}, 'Não foi possível solicitar a entrada.'),
      }))
    } finally {
      setBusyTeamId(null)
    }
  }

  return (
    <ul className="divide-y divide-line overflow-hidden rounded-md bg-surface ring-1 ring-line">
      {teams.map((team) => {
        const sent = sentTeamIds.includes(team.id)
        return (
          <li key={team.id} className="flex flex-col gap-2 px-4 py-3">
            <div className="flex items-center gap-3">
              <TeamBanner
                theme={team.bannerTheme}
                imageUrl={team.bannerImageUrl}
                className="w-12 shrink-0 rounded-md"
              />
              <p className="flex min-w-0 flex-1 items-center gap-1.5 truncate font-medium text-ink">
                {team.name}
                <Crown size={13} className="shrink-0 text-brand-green" aria-hidden="true" />
              </p>
              {sent ? (
                <span className="inline-flex shrink-0 items-center gap-1 text-xs font-medium text-brand-green">
                  <Check size={13} aria-hidden="true" />
                  Solicitado
                </span>
              ) : (
                <button
                  type="button"
                  disabled={busyTeamId === team.id}
                  onClick={() => handleRequest(team.id)}
                  className="inline-flex shrink-0 items-center rounded-xl bg-brand-green px-3 py-1.5 text-sm font-semibold text-brand-dark transition hover:brightness-95 disabled:opacity-60"
                >
                  Solicitar
                </button>
              )}
            </div>
            {requestErrors[team.id] && (
              <p role="alert" className="text-xs text-red-300">
                {requestErrors[team.id]}
              </p>
            )}
          </li>
        )
      })}
    </ul>
  )
}

export default TeamEntryPicker
