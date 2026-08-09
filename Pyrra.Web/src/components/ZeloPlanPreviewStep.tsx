import { WEEK_DAY_LABELS } from '../types/plan'
import { formatPlannedExercise } from '../utils/format'
import type { GeneratedPlanResponse } from '../types/zeloPlan'
import type { MealType } from '../types/nutrition'

interface ZeloPlanPreviewStepProps {
  plan: GeneratedPlanResponse
  applying: boolean
  error: string | null
  onApply: () => void
  onDiscard: () => void
}

// mesmos rótulos do NutritionPlanSection
const MEAL_LABELS: Record<MealType, string> = {
  CafeDaManha: 'Café da manhã',
  Almoco: 'Almoço',
  Lanche: 'Lanche',
  Jantar: 'Jantar',
}

// plano gerado inteiro (não expansível, é a única opção) — aceitar sobrescreve Treino e Nutrição, recusar descarta e mantém o que já existia
export function ZeloPlanPreviewStep({ plan, applying, error, onApply, onDiscard }: ZeloPlanPreviewStepProps) {
  return (
    <div className="flex flex-col gap-4">
      <p className="text-sm leading-relaxed text-slate-300">{plan.summary}</p>

      <section className="flex flex-col gap-2">
        <h3 className="text-xs font-semibold tracking-wide text-slate-500 uppercase">Treino</h3>
        <ul className="flex flex-col divide-y divide-line rounded-md bg-surface-hi ring-1 ring-line">
          {plan.workoutDays.map((day) => (
            <li key={day.dayOfWeek} className="px-3 py-2.5">
              <div className="flex items-baseline gap-2">
                <span className="w-16 shrink-0 text-xs font-medium text-slate-500">
                  {WEEK_DAY_LABELS[day.dayOfWeek]}
                </span>
                <span className="text-sm font-medium text-ink">{day.label ?? 'Descanso'}</span>
              </div>
              {day.exercises.length > 0 && (
                <ul className="mt-1 ml-16 flex flex-col gap-0.5">
                  {day.exercises.map((exercise, index) => (
                    <li key={index} className="text-xs text-slate-400">
                      {exercise.type === 'Corrida'
                        ? exercise.exerciseName
                        : formatPlannedExercise(exercise.exerciseName, exercise.sets, exercise.reps)}
                    </li>
                  ))}
                </ul>
              )}
            </li>
          ))}
        </ul>
      </section>

      <section className="flex flex-col gap-2">
        <h3 className="text-xs font-semibold tracking-wide text-slate-500 uppercase">Nutrição</h3>
        <ul className="flex flex-col divide-y divide-line rounded-md bg-surface-hi ring-1 ring-line">
          {plan.nutritionDays.map((day) => (
            <li key={day.dayOfWeek} className="px-3 py-2.5">
              <span className="text-xs font-medium text-slate-500">{WEEK_DAY_LABELS[day.dayOfWeek]}</span>
              <ul className="mt-1 flex flex-col gap-0.5">
                {day.items.map((item, index) => (
                  <li key={index} className="text-xs text-slate-400">
                    {MEAL_LABELS[item.mealType]}: {item.itemName} ({item.quantity})
                  </li>
                ))}
              </ul>
            </li>
          ))}
        </ul>
      </section>

      {error && (
        <p role="alert" className="text-sm text-red-300">
          {error}
        </p>
      )}

      <div className="flex gap-2">
        <button
          type="button"
          onClick={onDiscard}
          disabled={applying}
          className="flex-1 rounded-xl px-4 py-2.5 text-sm font-medium text-slate-400 ring-1 ring-line transition hover:bg-surface-hi hover:text-ink disabled:cursor-not-allowed disabled:opacity-60"
        >
          Descartar
        </button>
        <button
          type="button"
          onClick={onApply}
          disabled={applying}
          className="flex-1 rounded-xl bg-brand-green px-4 py-2.5 text-sm font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {applying ? 'Aplicando...' : 'Aplicar plano'}
        </button>
      </div>
    </div>
  )
}

export default ZeloPlanPreviewStep
