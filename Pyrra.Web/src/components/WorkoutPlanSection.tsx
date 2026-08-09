import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { LayoutTemplate, Plus, Sparkles, X } from 'lucide-react'
import SectionHeader from './SectionHeader'
import Segmented from './Segmented'
import WorkoutTemplatePicker from './WorkoutTemplatePicker'
import ZeloPlanModal from './ZeloPlanModal'
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

// campo vazio vira null (não informado), não 0
function parseCount(value: string): number | null {
  if (value.trim() === '') return null
  const parsed = Number(value)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null
}

// 7 linhas editáveis inline salvando no blur, mesmo padrão do card de reflexão
export function WorkoutPlanSection() {
  const [days, setDays] = useState<WorkoutPlanDayResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [state, setState] = useState<SaveState>('idle')
  const [error, setError] = useState<string | null>(null)
  // última versão confirmada pelo servidor, por dia — comparar evita PUT sem mudança real
  const savedLabels = useRef<Record<string, string>>({})

  // dia com a lista expandida, um por vez — sete listas abertas virariam uma tela inteira
  const [openDay, setOpenDay] = useState<WeekDay | null>(null)
  const [newExercise, setNewExercise] = useState('')
  const [newType, setNewType] = useState<WorkoutType>('Academia')
  const [newSets, setNewSets] = useState('')
  const [newReps, setNewReps] = useState('')
  const [adding, setAdding] = useState(false)
  const [removingId, setRemovingId] = useState<string | null>(null)

  // Modal de escolha de template.
  const [pickerOpen, setPickerOpen] = useState(false)

  // Modal do Zelo conversacional — mesma sessão do botão em Nutrição.
  const [zeloOpen, setZeloOpen] = useState(false)

  const today = todayWeekDay()

  // A semana "tem conteúdo" se algum dia tem label ou exercício — decide se aplicar um template
  // precisa confirmar a sobrescrita.
  const weekHasContent = days.some(
    (day) => (day.label ?? '').trim() !== '' || day.exercises.length > 0,
  )

  // Aplicar um template devolve o plano inteiro já montado: troca o estado local e a referência
  // de labels salvos, para os próximos blur não dispararem PUT à toa.
  function handleTemplateApplied(plan: WorkoutPlanDayResponse[]) {
    setDays(plan)
    savedLabels.current = Object.fromEntries(
      plan.map((day) => [day.dayOfWeek, day.label ?? '']),
    )
    setState('idle')
    setError(null)
  }

  // Aplicar o plano do Zelo não devolve o plano pronto como o template — rebusca do zero.
  async function handleZeloApplied() {
    try {
      const plan = await getWorkoutPlan()
      handleTemplateApplied(plan)
    } catch {
      // o modal do Zelo já confirmou a aplicação; se o refetch falhar, a próxima carga da tela mostra certo
    }
  }

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
      // só o nome é limpo, tipo/séries/reps ficam porque costuma-se repetir o esquema em sequência
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
        // silencioso: a seção some em vez de ocupar a tela com erro, o histórico abaixo continua normal
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
      // manda os 7 dias porque o endpoint é de plano inteiro, um PATCH do dia isolado não existe
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
          <div className="flex items-center gap-3">
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
            <button
              type="button"
              onClick={() => setZeloOpen(true)}
              className="inline-flex shrink-0 items-center gap-1 rounded-lg px-2.5 py-1 text-xs font-medium text-brand-green ring-1 ring-brand-green/30 transition hover:bg-brand-green/10"
            >
              <Sparkles size={13} aria-hidden="true" />
              Zelo
            </button>
            <button
              type="button"
              onClick={() => setPickerOpen(true)}
              className="inline-flex shrink-0 items-center gap-1 rounded-lg px-2.5 py-1 text-xs font-medium text-brand-green ring-1 ring-brand-green/30 transition hover:bg-brand-green/10"
            >
              <LayoutTemplate size={13} aria-hidden="true" />
              Aplicar template
            </button>
          </div>
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
                  // dia corrente ganha destaque, é a linha que importa ao abrir a tela pra treinar agora
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
                // sem moldura, o campo só se revela ao foco pra ler como lista e não formulário
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

      {pickerOpen && (
        <WorkoutTemplatePicker
          weekHasContent={weekHasContent}
          onApplied={handleTemplateApplied}
          onClose={() => setPickerOpen(false)}
        />
      )}

      {zeloOpen && (
        <ZeloPlanModal onApplied={handleZeloApplied} onClose={() => setZeloOpen(false)} />
      )}
    </section>
  )
}

export default WorkoutPlanSection
