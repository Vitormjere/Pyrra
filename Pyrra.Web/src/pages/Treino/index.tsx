import { useCallback, useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { Dumbbell, Footprints, Plus } from 'lucide-react'
import Segmented from '../../components/Segmented'
import { createWorkout, getWorkouts } from '../../services/workoutService'
import { getApiErrorMessage } from '../../services/apiError'
import { formatNumber, formatShortDate, todayIso } from '../../utils/format'
import type { WorkoutResponse, WorkoutType } from '../../types/workout'

const WORKOUT_TYPES: readonly WorkoutType[] = ['Academia', 'Corrida']

const inputClasses =
  'w-full rounded-xl bg-white/5 px-4 py-3 text-slate-100 ring-1 ring-white/10 transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green'

const labelClasses = 'text-xs font-medium text-slate-400'

// "60 kg · 4x10" ou "5 km em 30 min · 6 min/km"
function describeWorkout(workout: WorkoutResponse): string {
  if (workout.type === 'Academia') {
    const parts: string[] = []
    if (workout.loadKg !== null) parts.push(`${formatNumber(workout.loadKg)} kg`)
    if (workout.sets !== null && workout.reps !== null) {
      parts.push(`${workout.sets}x${workout.reps}`)
    }
    return parts.join(' · ')
  }

  const parts: string[] = []
  if (workout.distanceKm !== null && workout.durationMinutes !== null) {
    parts.push(
      `${formatNumber(workout.distanceKm)} km em ${workout.durationMinutes} min`,
    )
  }
  if (workout.paceMinPerKm !== null) {
    parts.push(`${formatNumber(workout.paceMinPerKm)} min/km`)
  }
  return parts.join(' · ')
}

// Campo vazio vira null (não informado), não 0.
function parseNumber(value: string): number | null {
  if (value.trim() === '') return null
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : null
}

function LoadingState() {
  return (
    <div className="flex flex-col gap-3" aria-busy="true" aria-label="Carregando">
      <div className="h-10 animate-pulse rounded-xl bg-white/5" />
      <div className="h-16 animate-pulse rounded-xl bg-white/5" />
      <div className="h-16 animate-pulse rounded-xl bg-white/5" />
    </div>
  )
}

export function Treino() {
  const [workouts, setWorkouts] = useState<WorkoutResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [formOpen, setFormOpen] = useState(false)
  const [type, setType] = useState<WorkoutType>('Academia')
  const [date, setDate] = useState(todayIso())
  const [exerciseName, setExerciseName] = useState('')
  const [loadKg, setLoadKg] = useState('')
  const [sets, setSets] = useState('')
  const [reps, setReps] = useState('')
  const [distanceKm, setDistanceKm] = useState('')
  const [durationMinutes, setDurationMinutes] = useState('')
  const [creating, setCreating] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)
  const firstFieldRef = useRef<HTMLInputElement>(null)

  const fetchWorkouts = useCallback(() => getWorkouts(), [])

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const data = await fetchWorkouts()
        if (active) setWorkouts(data)
      } catch (err) {
        if (active) {
          setError(
            getApiErrorMessage(err, {}, 'Não foi possível carregar seus treinos.'),
          )
        }
      } finally {
        if (active) setLoading(false)
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [fetchWorkouts])

  async function handleRetry() {
    setLoading(true)
    setError(null)
    try {
      setWorkouts(await fetchWorkouts())
    } catch (err) {
      setError(
        getApiErrorMessage(err, {}, 'Não foi possível carregar seus treinos.'),
      )
    } finally {
      setLoading(false)
    }
  }

  function resetFields() {
    setExerciseName('')
    setLoadKg('')
    setSets('')
    setReps('')
    setDistanceKm('')
    setDurationMinutes('')
  }

  async function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setCreateError(null)

    // Validação leve, só para poupar a ida ao servidor: o WorkoutService é a
    // autoridade sobre quais campos cada modalidade exige.
    if (type === 'Academia') {
      if (!exerciseName.trim()) {
        setCreateError('Informe o nome do exercício.')
        return
      }
      if (parseNumber(loadKg) === null) {
        setCreateError('Informe a carga em kg.')
        return
      }
    } else {
      if (parseNumber(distanceKm) === null) {
        setCreateError('Informe a distância em km.')
        return
      }
      if (parseNumber(durationMinutes) === null) {
        setCreateError('Informe a duração em minutos.')
        return
      }
    }

    setCreating(true)

    try {
      // Só os campos da modalidade escolhida são enviados: mandar os da outra
      // seria descartado pelo backend e confundiria a leitura do payload.
      const created = await createWorkout(
        type === 'Academia'
          ? {
              type,
              date,
              exerciseName: exerciseName.trim(),
              loadKg: parseNumber(loadKg),
              sets: parseNumber(sets),
              reps: parseNumber(reps),
            }
          : {
              type,
              date,
              distanceKm: parseNumber(distanceKm),
              durationMinutes: parseNumber(durationMinutes),
            },
      )

      setWorkouts((current) => [created, ...current])
      resetFields()
      firstFieldRef.current?.focus()
    } catch (err) {
      setCreateError(
        getApiErrorMessage(err, {}, 'Não foi possível registrar o treino.'),
      )
    } finally {
      setCreating(false)
    }
  }

  function closeForm() {
    setFormOpen(false)
    resetFields()
    setCreateError(null)
  }

  if (loading) return <LoadingState />

  if (error && workouts.length === 0) {
    return (
      <div className="flex flex-col items-center gap-4 py-12 text-center">
        <p className="text-sm text-red-300">{error}</p>
        <button
          type="button"
          onClick={handleRetry}
          className="rounded-xl bg-brand-green px-4 py-2.5 font-semibold text-brand-dark transition hover:brightness-95"
        >
          Tentar de novo
        </button>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-5">
      <header>
        <h1 className="font-display text-3xl tracking-tight">Treino</h1>
      </header>

      {formOpen ? (
        <form
          onSubmit={handleCreate}
          className="flex flex-col gap-3 rounded-xl bg-white/5 p-3 ring-1 ring-white/10"
        >
          <Segmented
            label="Tipo de treino"
            options={WORKOUT_TYPES}
            value={type}
            onChange={(next) => {
              setType(next)
              // Limpa ao trocar: os campos da modalidade anterior não se aplicam
              // à nova e seriam recusados pelo backend.
              resetFields()
              setCreateError(null)
            }}
          />

          <div className="flex flex-col gap-1">
            <label htmlFor="data-treino" className={labelClasses}>
              Data
            </label>
            <input
              id="data-treino"
              type="date"
              value={date}
              max={todayIso()}
              onChange={(event) => setDate(event.target.value)}
              className={inputClasses}
            />
          </div>

          {type === 'Academia' ? (
            <>
              <div className="flex flex-col gap-1">
                <label htmlFor="exercicio" className={labelClasses}>
                  Exercício
                </label>
                <input
                  id="exercicio"
                  ref={firstFieldRef}
                  type="text"
                  value={exerciseName}
                  onChange={(event) => setExerciseName(event.target.value)}
                  maxLength={200}
                  placeholder="Ex: supino reto"
                  className={inputClasses}
                />
              </div>

              <div className="grid grid-cols-3 gap-2">
                <div className="flex flex-col gap-1">
                  <label htmlFor="carga" className={labelClasses}>
                    Carga (kg)
                  </label>
                  <input
                    id="carga"
                    type="number"
                    inputMode="decimal"
                    step="0.5"
                    min="0"
                    value={loadKg}
                    onChange={(event) => setLoadKg(event.target.value)}
                    className={inputClasses}
                  />
                </div>
                <div className="flex flex-col gap-1">
                  <label htmlFor="series" className={labelClasses}>
                    Séries
                  </label>
                  <input
                    id="series"
                    type="number"
                    inputMode="numeric"
                    min="1"
                    value={sets}
                    onChange={(event) => setSets(event.target.value)}
                    className={inputClasses}
                  />
                </div>
                <div className="flex flex-col gap-1">
                  <label htmlFor="repeticoes" className={labelClasses}>
                    Reps
                  </label>
                  <input
                    id="repeticoes"
                    type="number"
                    inputMode="numeric"
                    min="1"
                    value={reps}
                    onChange={(event) => setReps(event.target.value)}
                    className={inputClasses}
                  />
                </div>
              </div>
            </>
          ) : (
            <>
              <div className="grid grid-cols-2 gap-2">
                <div className="flex flex-col gap-1">
                  <label htmlFor="distancia" className={labelClasses}>
                    Distância (km)
                  </label>
                  <input
                    id="distancia"
                    ref={firstFieldRef}
                    type="number"
                    inputMode="decimal"
                    step="0.1"
                    min="0"
                    value={distanceKm}
                    onChange={(event) => setDistanceKm(event.target.value)}
                    className={inputClasses}
                  />
                </div>
                <div className="flex flex-col gap-1">
                  <label htmlFor="duracao" className={labelClasses}>
                    Duração (min)
                  </label>
                  <input
                    id="duracao"
                    type="number"
                    inputMode="numeric"
                    min="1"
                    value={durationMinutes}
                    onChange={(event) => setDurationMinutes(event.target.value)}
                    className={inputClasses}
                  />
                </div>
              </div>
              <p className="text-xs text-slate-500">
                O pace é calculado automaticamente a partir da distância e da
                duração.
              </p>
            </>
          )}

          <div className="flex gap-2">
            <button
              type="submit"
              disabled={creating}
              className="flex-1 rounded-xl bg-brand-green px-4 py-2.5 font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {creating ? 'Salvando...' : 'Registrar treino'}
            </button>
            <button
              type="button"
              onClick={closeForm}
              className="rounded-xl px-4 py-2.5 font-medium text-slate-400 transition hover:bg-white/5 hover:text-slate-200"
            >
              Fechar
            </button>
          </div>

          {createError && (
            <p role="alert" className="text-sm text-red-300">
              {createError}
            </p>
          )}
        </form>
      ) : (
        <button
          type="button"
          onClick={() => setFormOpen(true)}
          className="flex min-h-12 w-full items-center justify-center gap-2 rounded-xl border-2 border-dashed border-white/15 text-sm font-medium text-slate-400 transition hover:border-white/25 hover:bg-white/5 hover:text-slate-200"
        >
          <Plus size={18} aria-hidden="true" />
          Registrar treino
        </button>
      )}

      <section className="flex flex-col gap-2">
        <h2 className="text-sm font-medium text-slate-300">Histórico</h2>

        {workouts.length > 0 ? (
          <ul className="flex flex-col gap-2">
            {workouts.map((workout) => {
              const Icon = workout.type === 'Academia' ? Dumbbell : Footprints
              return (
                <li
                  key={workout.id}
                  className="flex items-center gap-3 rounded-xl bg-white/5 px-4 py-3.5 ring-1 ring-white/10"
                >
                  <Icon
                    size={18}
                    className="shrink-0 text-brand-green"
                    aria-hidden="true"
                  />
                  <div className="min-w-0 flex-1">
                    <p className="truncate font-medium">
                      {workout.type === 'Academia'
                        ? (workout.exerciseName ?? 'Academia')
                        : 'Corrida'}
                    </p>
                    <p className="mt-0.5 text-xs text-slate-400">
                      {describeWorkout(workout)}
                    </p>
                  </div>
                  <span className="shrink-0 text-xs text-slate-500 tabular-nums">
                    {formatShortDate(workout.date)}
                  </span>
                </li>
              )
            })}
          </ul>
        ) : (
          <div className="rounded-2xl bg-white/5 px-5 py-8 text-center ring-1 ring-white/10">
            <p className="font-medium text-slate-200">Nenhum treino ainda.</p>
            <p className="mt-1.5 text-sm text-slate-400">
              Registre seu primeiro treino de academia ou corrida para começar a
              acompanhar sua evolução.
            </p>
          </div>
        )}
      </section>

      {error && workouts.length > 0 && (
        <p
          role="alert"
          className="rounded-lg bg-red-500/10 px-3 py-2 text-center text-sm text-red-300 ring-1 ring-red-500/20"
        >
          {error}
        </p>
      )}
    </div>
  )
}

export default Treino
