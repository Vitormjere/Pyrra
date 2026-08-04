import { useEffect, useState } from 'react'
import { ChevronDown, X } from 'lucide-react'
import {
  applyWorkoutTemplate,
  getWorkoutTemplates,
} from '../services/workoutService'
import { getApiErrorMessage } from '../services/apiError'
import { formatPlannedExercise } from '../utils/format'
import { useConfirm } from '../hooks/useConfirm'
import { WEEK_DAY_LABELS } from '../types/plan'
import type {
  WorkoutPlanDayResponse,
  WorkoutTemplate,
} from '../types/workout'

interface WorkoutTemplatePickerProps {
  // Se a semana já tem algo (label ou exercício), aplicar pede confirmação antes de sobrescrever.
  weekHasContent: boolean
  // Plano resultante da aplicação — o pai troca seu estado por este, sem refetch.
  onApplied: (plan: WorkoutPlanDayResponse[]) => void
  onClose: () => void
}

// Resumo de frequência do card: "6 dias de treino · 1 descanso". O template Personalizado
// (sem dias de treino) mostra só a descrição, que já diz o que ele faz.
function frequencyLabel(template: WorkoutTemplate): string {
  const treino = `${template.trainingDaysPerWeek} ${
    template.trainingDaysPerWeek === 1 ? 'dia de treino' : 'dias de treino'
  }`
  const descanso = `${template.restDaysPerWeek} descanso`
  return `${treino} · ${descanso}`
}

// Modal "Escolher template": lista os planos prontos em cards, cada um com preview expansível
// dos dias e exercícios. Segue o padrão de overlay do ConfirmDialog (fundo escuro, Esc/toque fora
// fecham). Ao aplicar, sobrescreve o Plano da Semana e devolve o resultado ao pai.
export function WorkoutTemplatePicker({
  weekHasContent,
  onApplied,
  onClose,
}: WorkoutTemplatePickerProps) {
  const [templates, setTemplates] = useState<WorkoutTemplate[]>([])
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [expandedId, setExpandedId] = useState<string | null>(null)
  const [applyingId, setApplyingId] = useState<string | null>(null)
  const [applyError, setApplyError] = useState<string | null>(null)
  const { confirm, dialog } = useConfirm()

  // Esc fecha, como no ConfirmDialog. Não fecha enquanto aplica: sair no meio deixaria a tela
  // sem saber o resultado da sobrescrita.
  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape' && applyingId === null) onClose()
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [onClose, applyingId])

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const data = await getWorkoutTemplates()
        if (active) setTemplates(data)
      } catch (err) {
        if (active) {
          setLoadError(
            getApiErrorMessage(err, {}, 'Não foi possível carregar os templates.'),
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
  }, [])

  async function handleApply(template: WorkoutTemplate) {
    // Personalizado não aplica estrutura: só fecha o modal e devolve o usuário ao editor manual,
    // que já está na tela. Nada é sobrescrito.
    if (template.isCustom) {
      onClose()
      return
    }

    if (weekHasContent) {
      const ok = await confirm({
        title: `Aplicar "${template.name}"?`,
        message:
          'Isso substitui os dias e exercícios que você já tem no Plano da Semana. Não dá para desfazer.',
        confirmLabel: 'Aplicar',
      })
      if (!ok) return
    }

    setApplyingId(template.id)
    setApplyError(null)

    try {
      const plan = await applyWorkoutTemplate(template.id)
      onApplied(plan)
      onClose()
    } catch (err) {
      setApplyError(
        getApiErrorMessage(err, {}, 'Não foi possível aplicar o template.'),
      )
      setApplyingId(null)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <button
        type="button"
        aria-label="Fechar"
        onClick={() => applyingId === null && onClose()}
        className="absolute inset-0 bg-brand-dark/80 backdrop-blur-sm"
      />

      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="template-picker-title"
        className="relative flex max-h-[85vh] w-full max-w-lg flex-col rounded-md bg-surface ring-1 ring-line"
      >
        <div className="flex items-center justify-between border-b border-line px-5 py-4">
          <div>
            <h2
              id="template-picker-title"
              className="font-display text-xl font-semibold tracking-tight text-ink"
            >
              Escolher template
            </h2>
            <p className="mt-0.5 text-sm text-slate-400">
              Planos prontos para preencher sua semana de uma vez.
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            disabled={applyingId !== null}
            aria-label="Fechar"
            className="shrink-0 rounded p-1 text-slate-500 transition hover:bg-surface-hi hover:text-ink disabled:opacity-50"
          >
            <X size={20} />
          </button>
        </div>

        <div className="flex-1 overflow-y-auto px-5 py-4">
          {loading && (
            <p className="py-8 text-center text-sm text-slate-500">
              Carregando templates...
            </p>
          )}

          {loadError && (
            <p role="alert" className="py-8 text-center text-sm text-red-300">
              {loadError}
            </p>
          )}

          {!loading && !loadError && (
            <ul className="flex flex-col gap-3">
              {templates.map((template) => {
                const expanded = expandedId === template.id
                const applying = applyingId === template.id

                return (
                  <li
                    key={template.id}
                    className="overflow-hidden rounded-md bg-surface-hi ring-1 ring-line"
                  >
                    <div className="flex flex-col gap-3 p-4">
                      <div className="flex items-start gap-3">
                        <div className="min-w-0 flex-1">
                          <p className="font-semibold text-ink">{template.name}</p>
                          <p className="mt-0.5 text-xs font-medium text-brand-green">
                            {template.isCustom
                              ? 'Montar do zero'
                              : frequencyLabel(template)}
                          </p>
                          <p className="mt-1 text-sm text-slate-400">
                            {template.description}
                          </p>
                        </div>
                      </div>

                      {/* Personalizado não tem estrutura para prever: mostra só o botão. */}
                      {!template.isCustom && (
                        <button
                          type="button"
                          onClick={() =>
                            setExpandedId(expanded ? null : template.id)
                          }
                          aria-expanded={expanded}
                          className="flex items-center gap-1 self-start text-xs font-medium text-slate-400 transition hover:text-ink"
                        >
                          <ChevronDown
                            size={14}
                            className={[
                              'transition-transform',
                              expanded ? 'rotate-180' : '',
                            ].join(' ')}
                          />
                          {expanded ? 'Ocultar dias' : 'Ver dias'}
                        </button>
                      )}

                      {expanded && !template.isCustom && (
                        <ul className="flex flex-col gap-2 border-t border-line pt-3">
                          {template.days.map((day) => (
                            <li key={day.dayOfWeek} className="flex flex-col gap-1">
                              <div className="flex items-baseline gap-2">
                                <span className="w-16 shrink-0 text-xs font-medium text-slate-500">
                                  {WEEK_DAY_LABELS[day.dayOfWeek]}
                                </span>
                                <span className="text-sm font-medium text-ink">
                                  {day.label}
                                </span>
                              </div>
                              {day.exercises.length > 0 && (
                                <ul className="ml-16 flex flex-col gap-0.5">
                                  {day.exercises.map((exercise, index) => (
                                    <li
                                      key={index}
                                      className="text-xs text-slate-400"
                                    >
                                      {formatPlannedExercise(
                                        exercise.exerciseName,
                                        exercise.sets,
                                        exercise.reps,
                                      )}
                                    </li>
                                  ))}
                                </ul>
                              )}
                            </li>
                          ))}
                        </ul>
                      )}

                      <button
                        type="button"
                        onClick={() => handleApply(template)}
                        disabled={applyingId !== null}
                        className="self-start rounded-xl bg-brand-green px-4 py-2 text-sm font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
                      >
                        {applying
                          ? 'Aplicando...'
                          : template.isCustom
                            ? 'Montar do zero'
                            : 'Aplicar template'}
                      </button>
                    </div>
                  </li>
                )
              })}
            </ul>
          )}

          {applyError && (
            <p role="alert" className="mt-3 text-center text-sm text-red-300">
              {applyError}
            </p>
          )}
        </div>
      </div>

      {dialog}
    </div>
  )
}

export default WorkoutTemplatePicker
