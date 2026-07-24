import { useState } from 'react'
import type { FormEvent } from 'react'
import { ChevronDown, ChevronRight } from 'lucide-react'
import ItemActions from './ItemActions'
import {
  deleteWorkout,
  getWorkoutHistory,
  updateWorkout,
} from '../services/workoutService'
import { getApiErrorMessage } from '../services/apiError'
import { formatNumber, formatShortDate } from '../utils/format'
import type { WorkoutResponse } from '../types/workout'

interface GymHistorySectionProps {
  /** Todos os treinos do usuário; a filtragem por Academia é feita aqui. */
  workouts: WorkoutResponse[]
  /** Sincroniza a lista mestre da página quando uma entrada é editada aqui. */
  onWorkoutUpdated: (updated: WorkoutResponse) => void
  /** Sincroniza a lista mestre da página quando uma entrada é removida aqui. */
  onWorkoutDeleted: (workoutId: string) => void
}

// Campo vazio ou não numérico vira null.
function parseNumber(value: string): number | null {
  if (value.trim() === '') return null
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : null
}

// Histórico de Academia agrupado por exercício, para ler evolução de carga —
// a lista cronológica plana misturava exercícios diferentes e não respondia
// "quanto eu levantava antes neste movimento?".
export function GymHistorySection({
  workouts,
  onWorkoutUpdated,
  onWorkoutDeleted,
}: GymHistorySectionProps) {
  // Cache por exercício: reabrir um já consultado não refaz a requisição.
  const [historyByExercise, setHistoryByExercise] = useState<
    Record<string, WorkoutResponse[]>
  >({})
  const [openExercise, setOpenExercise] = useState<string | null>(null)
  const [loadingExercise, setLoadingExercise] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  // Edição inline de uma entrada (carga + data) e travas de linha em voo.
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editLoad, setEditLoad] = useState('')
  const [editDate, setEditDate] = useState('')
  const [savingEdit, setSavingEdit] = useState(false)
  const [editError, setEditError] = useState<string | null>(null)
  const [deletingId, setDeletingId] = useState<string | null>(null)

  function startEdit(entry: WorkoutResponse) {
    setEditingId(entry.id)
    setEditLoad(entry.loadKg !== null ? String(entry.loadKg) : '')
    setEditDate(entry.date)
    setEditError(null)
  }

  // Substitui a entrada no cache do exercício e reordena por data crescente —
  // editar a data pode mudar a posição na progressão.
  function replaceInCache(exerciseName: string, updated: WorkoutResponse) {
    setHistoryByExercise((current) => {
      const list = current[exerciseName]
      if (!list) return current
      const next = list
        .map((entry) => (entry.id === updated.id ? updated : entry))
        .sort((a, b) => a.date.localeCompare(b.date))
      return { ...current, [exerciseName]: next }
    })
  }

  function removeFromCache(exerciseName: string, workoutId: string) {
    setHistoryByExercise((current) => {
      const list = current[exerciseName]
      if (!list) return current
      return {
        ...current,
        [exerciseName]: list.filter((entry) => entry.id !== workoutId),
      }
    })
  }

  async function handleSaveEdit(
    event: FormEvent<HTMLFormElement>,
    entry: WorkoutResponse,
    exerciseName: string,
  ) {
    event.preventDefault()

    if (parseNumber(editLoad) === null) {
      setEditError('Informe a carga em kg.')
      return
    }

    setSavingEdit(true)
    setEditError(null)

    try {
      // O nome do exercício é mantido (é o grupo em que a linha vive); só carga e
      // data são editáveis aqui.
      const updated = await updateWorkout(entry.id, {
        type: 'Academia',
        date: editDate,
        exerciseName,
        loadKg: parseNumber(editLoad),
      })
      replaceInCache(exerciseName, updated)
      onWorkoutUpdated(updated)
      setEditingId(null)
    } catch (err) {
      setEditError(
        getApiErrorMessage(err, {}, 'Não foi possível salvar o treino.'),
      )
    } finally {
      setSavingEdit(false)
    }
  }

  async function handleDelete(entry: WorkoutResponse, exerciseName: string) {
    if (!window.confirm(`Remover o registro de ${formatShortDate(entry.date)}?`)) {
      return
    }

    setDeletingId(entry.id)
    setError(null)

    try {
      await deleteWorkout(entry.id)
      removeFromCache(exerciseName, entry.id)
      onWorkoutDeleted(entry.id)
      if (editingId === entry.id) setEditingId(null)
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível remover o treino.'))
    } finally {
      setDeletingId(null)
    }
  }

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
                      {history.map((entry) =>
                        editingId === entry.id ? (
                          <li key={entry.id}>
                            <form
                              onSubmit={(event) =>
                                handleSaveEdit(event, entry, name)
                              }
                              className="flex flex-col gap-2 py-1"
                            >
                              <div className="grid grid-cols-2 gap-2">
                                <input
                                  type="number"
                                  inputMode="decimal"
                                  step="0.5"
                                  min="0"
                                  value={editLoad}
                                  onChange={(event) => {
                                    setEditLoad(event.target.value)
                                    if (editError !== null) setEditError(null)
                                  }}
                                  autoFocus
                                  aria-label="Carga em kg"
                                  className="w-full rounded-md bg-surface px-3 py-2 text-xs text-ink ring-1 ring-line outline-none focus:ring-2 focus:ring-brand-green"
                                />
                                <input
                                  type="date"
                                  value={editDate}
                                  onChange={(event) =>
                                    setEditDate(event.target.value)
                                  }
                                  aria-label="Data"
                                  className="w-full rounded-md bg-surface px-3 py-2 text-xs text-ink ring-1 ring-line outline-none focus:ring-2 focus:ring-brand-green"
                                />
                              </div>
                              <div className="flex gap-2">
                                <button
                                  type="submit"
                                  disabled={savingEdit}
                                  className="flex-1 rounded-lg bg-brand-green px-3 py-1.5 text-xs font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
                                >
                                  {savingEdit ? 'Salvando...' : 'Salvar'}
                                </button>
                                <button
                                  type="button"
                                  onClick={() => setEditingId(null)}
                                  className="rounded-md px-3 py-1.5 text-xs font-medium text-slate-400 transition hover:bg-surface-hi hover:text-slate-200"
                                >
                                  Cancelar
                                </button>
                              </div>
                              {editError && (
                                <p role="alert" className="text-xs text-red-300">
                                  {editError}
                                </p>
                              )}
                            </form>
                          </li>
                        ) : (
                          <li
                            key={entry.id}
                            className="flex items-center gap-3 text-xs"
                          >
                            <span className="text-slate-500 tabular-nums">
                              {formatShortDate(entry.date)}
                            </span>
                            <span className="flex-1 text-slate-300 tabular-nums">
                              {entry.loadKg !== null
                                ? `${formatNumber(entry.loadKg)} kg`
                                : '—'}
                            </span>
                            <ItemActions
                              busy={deletingId === entry.id}
                              onEdit={() => startEdit(entry)}
                              onDelete={() => handleDelete(entry, name)}
                              editLabel="Editar registro"
                              deleteLabel="Remover registro"
                            />
                          </li>
                        ),
                      )}
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
