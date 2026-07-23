import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { Plus, X } from 'lucide-react'
import SectionHeader from './SectionHeader'
import Segmented from './Segmented'
import {
  addPlanExercise,
  getWorkoutPlan,
  removePlanExercise,
  saveWorkoutPlan,
} from '../services/workoutService'
import { formatPlannedExercise } from '../utils/format'
import { getApiErrorMessage } from '../services/apiError'
import { WEEK_DAY_LABELS, todayWeekDay } from '../types/plan'
import type { WeekDay } from '../types/plan'
import type { WorkoutPlanDayResponse, WorkoutType } from '../types/workout'

type SaveState = 'idle' | 'saving' | 'saved' | 'error'

const WORKOUT_TYPES: readonly WorkoutType[] = ['Academia', 'Corrida']

const exerciseInputClasses =
  'w-full rounded-md bg-surface-hi px-3 py-1.5 text-sm text-ink ring-1 ring-line transition outline-none placeholder:text-slate-600 focus:ring-2 focus:ring-brand-green'

// Campo vazio vira null (não informado), não 0.
function parseCount(value: string): number | null {
  if (value.trim() === '') return null
  const parsed = Number(value)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null
}

// Plano semanal de treino: 7 linhas editáveis inline, salvando ao sair do campo
// — mesmo padrão do card de reflexão. O backend sempre devolve os 7 dias, então
// a tela não precisa completar buracos.
export function WorkoutPlanSection() {
  const [days, setDays] = useState<WorkoutPlanDayResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [state, setState] = useState<SaveState>('idle')
  const [error, setError] = useState<string | null>(null)
  // Última versão confirmada pelo servidor, por dia. Comparar contra ela evita
  // PUT quando o usuário só entra e sai do campo sem digitar.
  const savedLabels = useRef<Record<string, string>>({})

  // Dia com a lista de exercícios expandida. Um por vez: sete listas abertas
  // transformariam a seção numa tela inteira.
  const [openDay, setOpenDay] = useState<WeekDay | null>(null)
  const [newExercise, setNewExercise] = useState('')
  const [newType, setNewType] = useState<WorkoutType>('Academia')
  const [newSets, setNewSets] = useState('')
  const [newReps, setNewReps] = useState('')
  const [adding, setAdding] = useState(false)
  const [removingId, setRemovingId] = useState<string | null>(null)

  const today = todayWeekDay()

  async function handleAddExercise(
    event: FormEvent<HTMLFormElement>,
    day: WeekDay,
  ) {
    event.preventDefault()

    const name = newExercise.trim()
    if (!name) return

    setAdding(true)
    setError(null)

    try {
      const created = await addPlanExercise(
        day,
        newType,
        name,
        parseCount(newSets),
        parseCount(newReps),
      )
      setDays((current) =>
        current.map((item) =>
          item.dayOfWeek === day
            ? { ...item, exercises: [...item.exercises, created] }
            : item,
        ),
      )
      // Só o nome é limpo: tipo, séries e reps permanecem, porque quem monta um
      // dia costuma repetir o mesmo esquema em vários exercícios seguidos.
      setNewExercise('')
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível adicionar.'))
    } finally {
      setAdding(false)
    }
  }

  async function handleRemoveExercise(exerciseId: string) {
    setRemovingId(exerciseId)
    setError(null)

    try {
      await removePlanExercise(exerciseId)
      setDays((current) =>
        current.map((item) => ({
          ...item,
          exercises: item.exercises.filter((e) => e.id !== exerciseId),
        })),
      )
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível remover.'))
    } finally {
      setRemovingId(null)
    }
  }

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const plan = await getWorkoutPlan()
        if (!active) return
        setDays(plan)
        savedLabels.current = Object.fromEntries(
          plan.map((day) => [day.dayOfWeek, day.label ?? '']),
        )
      } catch {
        // Silencioso: a seção some em vez de ocupar a tela com erro. O histórico
        // abaixo continua funcionando normalmente.
        if (active) setError('unavailable')
      } finally {
        if (active) setLoading(false)
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [])

  function updateLabel(day: WeekDay, label: string) {
    setDays((current) =>
      current.map((item) =>
        item.dayOfWeek === day ? { ...item, label } : item,
      ),
    )
    if (state !== 'idle') setState('idle')
  }

  async function handleBlur(day: WeekDay) {
    const current = days.find((item) => item.dayOfWeek === day)
    if (!current) return

    const label = current.label ?? ''
    if (label === savedLabels.current[day]) return

    setState('saving')
    setError(null)

    try {
      // Manda os 7 dias porque o endpoint é de plano inteiro. Enviar só o dia
      // alterado exigiria um PATCH que o backend não expõe.
      const saved = await saveWorkoutPlan(days)
      setDays(saved)
      savedLabels.current = Object.fromEntries(
        saved.map((item) => [item.dayOfWeek, item.label ?? '']),
      )
      setState('saved')
    } catch (err) {
      setState('error')
      setError(getApiErrorMessage(err, {}, 'Não foi possível salvar.'))
    }
  }

  if (loading || error === 'unavailable') return null

  return (
    <section className="flex flex-col gap-2">
      <SectionHeader
        trailing={
          <span
            aria-live="polite"
            className={[
              'text-[11px]',
              state === 'error' ? 'text-red-300' : 'text-slate-500',
            ].join(' ')}
          >
            {state === 'saving' && 'Salvando...'}
            {state === 'saved' && 'Salvo'}
            {state === 'error' && (error ?? 'Erro ao salvar')}
          </span>
        }
      >
        Plano da semana
      </SectionHeader>

      <ul className="divide-y divide-line overflow-hidden rounded-md bg-surface ring-1 ring-line">
        {days.map((day) => (
          <li key={day.dayOfWeek} className="px-4 py-2.5">
            <div className="flex items-center gap-3">
              <label
                htmlFor={`plano-${day.dayOfWeek}`}
                className={[
                  'w-20 shrink-0 text-xs font-medium',
                  // O dia corrente ganha a cor de destaque: é a linha que importa
                  // quando se abre a tela para treinar agora.
                  day.dayOfWeek === today ? 'text-brand-green' : 'text-slate-500',
                ].join(' ')}
              >
                {WEEK_DAY_LABELS[day.dayOfWeek]}
              </label>
              <input
                id={`plano-${day.dayOfWeek}`}
                type="text"
                value={day.label ?? ''}
                onChange={(event) => updateLabel(day.dayOfWeek, event.target.value)}
                onBlur={() => handleBlur(day.dayOfWeek)}
                maxLength={200}
                placeholder="Sem plano definido"
                // Sem moldura: o campo só se revela ao foco, para as 7 linhas
                // lerem como lista e não como formulário.
                className="w-full rounded bg-transparent px-2 py-1.5 text-sm text-ink transition outline-none placeholder:text-slate-600 focus:bg-surface-hi focus:ring-1 focus:ring-brand-green"
              />
              <button
                type="button"
                onClick={() =>
                  setOpenDay(openDay === day.dayOfWeek ? null : day.dayOfWeek)
                }
                aria-expanded={openDay === day.dayOfWeek}
                aria-label={`Exercícios de ${WEEK_DAY_LABELS[day.dayOfWeek]}`}
                className="shrink-0 rounded p-1 text-slate-500 transition hover:bg-surface-hi hover:text-brand-green"
              >
                {/* O contador aparece mesmo com o dia fechado: sem ele não dá
                    para saber quais dias têm exercícios sem abrir um a um. */}
                {day.exercises.length > 0 ? (
                  <span className="px-1 text-xs font-semibold tabular-nums">
                    {day.exercises.length}
                  </span>
                ) : (
                  <Plus size={15} />
                )}
              </button>
            </div>

            {openDay === day.dayOfWeek && (
              <div className="mt-2 pl-2">
                {day.exercises.length > 0 && (
                  <ul className="mb-2 flex flex-col gap-1">
                    {day.exercises.map((exercise) => (
                      <li
                        key={exercise.id}
                        className="flex items-center gap-2 text-sm"
                      >
                        <span className="min-w-0 flex-1 truncate text-ink">
                          {formatPlannedExercise(
                            exercise.exerciseName,
                            exercise.sets,
                            exercise.reps,
                          )}
                        </span>
                        <button
                          type="button"
                          disabled={removingId === exercise.id}
                          onClick={() => handleRemoveExercise(exercise.id)}
                          aria-label={`Remover ${exercise.exerciseName}`}
                          className="shrink-0 rounded p-1 text-slate-600 transition hover:text-red-400 disabled:opacity-50"
                        >
                          <X size={13} />
                        </button>
                      </li>
                    ))}
                  </ul>
                )}

                <form
                  onSubmit={(event) => handleAddExercise(event, day.dayOfWeek)}
                  className="flex flex-col gap-2"
                >
                  <Segmented
                    label="Modalidade"
                    options={WORKOUT_TYPES}
                    value={newType}
                    onChange={setNewType}
                  />

                  <div className="flex gap-2">
                    <input
                      type="text"
                      value={newExercise}
                      onChange={(event) => setNewExercise(event.target.value)}
                      maxLength={200}
                      placeholder={
                        newType === 'Academia'
                          ? 'Exercício (ex: supino reto)'
                          : 'Treino (ex: 5km leve)'
                      }
                      aria-label={
                        newType === 'Academia' ? 'Nome do exercício' : 'Descrição do treino'
                      }
                      className={exerciseInputClasses}
                    />
                    <button
                      type="submit"
                      disabled={adding || newExercise.trim().length === 0}
                      className="shrink-0 rounded-xl bg-brand-green px-3 py-1.5 text-sm font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      {adding ? '...' : 'Add'}
                    </button>
                  </div>

                  {/* Séries e repetições só existem em Academia — em Corrida os
                      campos somem em vez de ficarem desabilitados, para não
                      sugerir que há algo a preencher ali. */}
                  {newType === 'Academia' && (
                    <div className="flex gap-2">
                      <input
                        type="number"
                        inputMode="numeric"
                        min="1"
                        value={newSets}
                        onChange={(event) => setNewSets(event.target.value)}
                        placeholder="Séries"
                        aria-label="Séries"
                        className={exerciseInputClasses}
                      />
                      <input
                        type="number"
                        inputMode="numeric"
                        min="1"
                        value={newReps}
                        onChange={(event) => setNewReps(event.target.value)}
                        placeholder="Reps"
                        aria-label="Repetições"
                        className={exerciseInputClasses}
                      />
                    </div>
                  )}
                </form>
              </div>
            )}
          </li>
        ))}
      </ul>
    </section>
  )
}

export default WorkoutPlanSection
