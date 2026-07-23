import { useState } from 'react'
import { ChevronDown, ChevronRight } from 'lucide-react'
import { getWorkoutHistory } from '../services/workoutService'
import { getApiErrorMessage } from '../services/apiError'
import { formatNumber, formatShortDate } from '../utils/format'
import type { WorkoutResponse } from '../types/workout'

interface GymHistorySectionProps {
  /** Todos os treinos do usuário; a filtragem por Academia é feita aqui. */
  workouts: WorkoutResponse[]
}

// Histórico de Academia agrupado por exercício, para ler evolução de carga —
// a lista cronológica plana misturava exercícios diferentes e não respondia
// "quanto eu levantava antes neste movimento?".
export function GymHistorySection({ workouts }: GymHistorySectionProps) {
  // Cache por exercício: reabrir um já consultado não refaz a requisição.
  const [historyByExercise, setHistoryByExercise] = useState<
    Record<string, WorkoutResponse[]>
  >({})
  const [openExercise, setOpenExercise] = useState<string | null>(null)
  const [loadingExercise, setLoadingExercise] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  // Nomes distintos, na ordem em que aparecem (mais recente primeiro, que é
  // como o backend devolve a lista completa).
  const exerciseNames = Array.from(
    new Set(
      workouts
        .filter((w) => w.type === 'Academia' && w.exerciseName !== null)
        .map((w) => w.exerciseName as string),
    ),
  )

  async function toggleExercise(name: string) {
    if (openExercise === name) {
      setOpenExercise(null)
      return
    }

    setOpenExercise(name)

    if (historyByExercise[name]) return

    setLoadingExercise(name)
    setError(null)

    try {
      // Endpoint dedicado: devolve o histórico daquele exercício em ordem
      // CRESCENTE de data, que é como se lê progressão de carga.
      const history = await getWorkoutHistory(name)
      setHistoryByExercise((current) => ({ ...current, [name]: history }))
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível carregar o histórico.'))
      setOpenExercise(null)
    } finally {
      setLoadingExercise(null)
    }
  }

  if (exerciseNames.length === 0) {
    return (
      <div className="rounded-md bg-surface px-5 py-8 text-center ring-1 ring-line">
        <p className="text-sm text-slate-400">
          Nenhum treino de academia registrado ainda.
        </p>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-2">
      <ul className="divide-y divide-line overflow-hidden rounded-md bg-surface ring-1 ring-line">
        {exerciseNames.map((name) => {
          const expanded = openExercise === name
          const history = historyByExercise[name]

          return (
            <li key={name}>
              <button
                type="button"
                onClick={() => toggleExercise(name)}
                aria-expanded={expanded}
                className="flex w-full items-center gap-3 px-4 py-3.5 text-left transition hover:bg-surface-hi"
              >
                {expanded ? (
                  <ChevronDown size={15} className="shrink-0 text-slate-500" aria-hidden="true" />
                ) : (
                  <ChevronRight size={15} className="shrink-0 text-slate-500" aria-hidden="true" />
                )}
                <span className="min-w-0 flex-1 truncate text-ink">{name}</span>
              </button>

              {expanded && (
                <div className="px-4 pb-3 pl-12">
                  {loadingExercise === name ? (
                    <p className="text-xs text-slate-500">Carregando...</p>
                  ) : history && history.length > 0 ? (
                    <ul className="flex flex-col gap-1">
                      {history.map((entry) => (
                        <li
                          key={entry.id}
                          className="flex items-center gap-3 text-xs"
                        >
                          <span className="text-slate-500 tabular-nums">
                            {formatShortDate(entry.date)}
                          </span>
                          <span className="text-slate-300 tabular-nums">
                            {entry.loadKg !== null
                              ? `${formatNumber(entry.loadKg)} kg`
                              : '—'}
                          </span>
                        </li>
                      ))}
                    </ul>
                  ) : (
                    <p className="text-xs text-slate-500">Sem registros.</p>
                  )}
                </div>
              )}
            </li>
          )
        })}
      </ul>

      {error && (
        <p
          role="alert"
          className="rounded-md bg-red-500/10 px-3 py-2 text-center text-sm text-red-300 ring-1 ring-red-500/20"
        >
          {error}
        </p>
      )}
    </div>
  )
}

export default GymHistorySection
