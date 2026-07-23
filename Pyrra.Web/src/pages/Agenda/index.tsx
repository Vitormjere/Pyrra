import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ChevronLeft, ChevronRight, Plus, X } from 'lucide-react'
import SectionHeader from '../../components/SectionHeader'
import { getTasksForRange } from '../../services/taskService'
import { getWorkoutsForRange } from '../../services/workoutService'
import { getEntriesForRange } from '../../services/financeService'
import { getCategories } from '../../services/financeService'
import { getApiErrorMessage } from '../../services/apiError'
import {
  formatCurrency,
  formatDayLabel,
  formatNumber,
  toIsoDate,
  todayIso,
} from '../../utils/format'
import type { TaskResponse } from '../../types/task'
import type { WorkoutResponse } from '../../types/workout'
import type {
  FinanceCategoryResponse,
  FinanceEntryResponse,
} from '../../types/finance'

const WEEKDAY_LABELS = ['S', 'T', 'Q', 'Q', 'S', 'S', 'D']

/*
  Paleta dos marcadores.

  Os pontos têm 6px: mesmo coloridos, a área de cor no calendário é mínima, o
  que mantém a leitura monocromática da tela. Verde fica com Tarefa por ser o
  acento que o app já usa para "o que você se comprometeu a fazer"; treino e
  financeiro recebem hues frios/quentes distintos apenas para serem
  distinguíveis entre si — nenhum deles aparece como preenchimento em nenhum
  outro lugar da Agenda.
*/
const DOT_COLORS = {
  task: 'bg-brand-green',
  workout: 'bg-sky-400',
  finance: 'bg-amber-400',
} as const

const TEXT_COLORS = {
  task: 'text-brand-green',
  workout: 'text-sky-400',
  finance: 'text-amber-400',
} as const

// Grade do mês com a semana começando na SEGUNDA, igual ao resto do app
// (WeekRange no backend). Células nulas preenchem o espaço antes do dia 1.
function buildMonthGrid(year: number, month: number): (string | null)[] {
  const firstWeekday = (new Date(year, month, 1).getDay() + 6) % 7
  // Dia 0 do mês seguinte = último dia deste mês.
  const daysInMonth = new Date(year, month + 1, 0).getDate()

  const cells: (string | null)[] = Array<string | null>(firstWeekday).fill(null)
  for (let day = 1; day <= daysInMonth; day += 1) {
    cells.push(toIsoDate(new Date(year, month, day)))
  }
  return cells
}

function monthLabel(year: number, month: number): string {
  return new Intl.DateTimeFormat('pt-BR', {
    month: 'long',
    year: 'numeric',
  }).format(new Date(year, month, 1))
}

function LoadingState() {
  return (
    <div className="flex flex-col gap-3" aria-busy="true" aria-label="Carregando">
      <div className="h-10 animate-pulse rounded-md bg-surface" />
      <div className="h-64 animate-pulse rounded-md bg-surface" />
    </div>
  )
}

export function Agenda() {
  const today = todayIso()
  const now = new Date()

  const [year, setYear] = useState(now.getFullYear())
  const [month, setMonth] = useState(now.getMonth())
  const [selectedDate, setSelectedDate] = useState<string>(today)

  const [tasks, setTasks] = useState<TaskResponse[]>([])
  const [workouts, setWorkouts] = useState<WorkoutResponse[]>([])
  const [entries, setEntries] = useState<FinanceEntryResponse[]>([])
  const [categories, setCategories] = useState<FinanceCategoryResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [addMenuOpen, setAddMenuOpen] = useState(false)

  const cells = buildMonthGrid(year, month)

  // Intervalo do mês inteiro: a primeira e a última célula preenchidas.
  const rangeStart = toIsoDate(new Date(year, month, 1))
  const rangeEnd = toIsoDate(new Date(year, month + 1, 0))

  const fetchMonth = useCallback(async (start: string, end: string) => {
    // Três consultas de mesmo formato, em paralelo. As categorias vêm junto
    // porque o lançamento só carrega o categoryId.
    const [tasksData, workoutsData, entriesData, categoriesData] =
      await Promise.all([
        getTasksForRange(start, end),
        getWorkoutsForRange(start, end),
        getEntriesForRange(start, end),
        getCategories(),
      ])
    return { tasksData, workoutsData, entriesData, categoriesData }
  }, [])

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const result = await fetchMonth(rangeStart, rangeEnd)
        if (!active) return
        setTasks(result.tasksData)
        setWorkouts(result.workoutsData)
        setEntries(result.entriesData)
        setCategories(result.categoriesData)
        setError(null)
      } catch (err) {
        if (active) {
          setError(
            getApiErrorMessage(err, {}, 'Não foi possível carregar a agenda.'),
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
  }, [fetchMonth, rangeStart, rangeEnd])

  function goToPreviousMonth() {
    setLoading(true)
    if (month === 0) {
      setYear((value) => value - 1)
      setMonth(11)
    } else {
      setMonth((value) => value - 1)
    }
  }

  function goToNextMonth() {
    setLoading(true)
    if (month === 11) {
      setYear((value) => value + 1)
      setMonth(0)
    } else {
      setMonth((value) => value + 1)
    }
  }

  const dayTasks = tasks.filter((task) => task.date === selectedDate)
  const dayWorkouts = workouts.filter((workout) => workout.date === selectedDate)
  const dayEntries = entries.filter((entry) => entry.date === selectedDate)
  const hasItems =
    dayTasks.length > 0 || dayWorkouts.length > 0 || dayEntries.length > 0

  function categoryNameOf(entry: FinanceEntryResponse): string {
    return (
      categories.find((category) => category.id === entry.categoryId)?.name ??
      'Sem categoria'
    )
  }

  return (
    <div className="flex flex-col gap-5">
      <header>
        <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">Agenda</h1>
      </header>

      {/* NAVEGAÇÃO DE MÊS */}
      <div className="flex items-center justify-between">
        <button
          type="button"
          onClick={goToPreviousMonth}
          aria-label="Mês anterior"
          className="rounded-lg p-2 text-slate-400 transition hover:bg-surface hover:text-ink"
        >
          <ChevronLeft size={18} />
        </button>
        <p className="text-sm font-medium first-letter:uppercase">
          {monthLabel(year, month)}
        </p>
        <button
          type="button"
          onClick={goToNextMonth}
          aria-label="Próximo mês"
          className="rounded-lg p-2 text-slate-400 transition hover:bg-surface hover:text-ink"
        >
          <ChevronRight size={18} />
        </button>
      </div>

      {loading ? (
        <LoadingState />
      ) : (
        <>
          {/* GRADE DO MÊS */}
          <div className="rounded-md bg-surface p-3 ring-1 ring-line">
            <div className="grid grid-cols-7 gap-1">
              {WEEKDAY_LABELS.map((label, index) => (
                <div
                  key={`${label}-${index}`}
                  className="pb-1 text-center text-[10px] font-semibold tracking-wider text-slate-600 uppercase"
                >
                  {label}
                </div>
              ))}

              {cells.map((date, index) => {
                if (date === null) {
                  return <div key={`empty-${index}`} />
                }

                const hasTask = tasks.some((task) => task.date === date)
                const hasWorkout = workouts.some(
                  (workout) => workout.date === date,
                )
                const hasEntry = entries.some((entry) => entry.date === date)

                const isSelected = date === selectedDate
                const isToday = date === today
                const dayNumber = Number(date.slice(8, 10))

                return (
                  <button
                    key={date}
                    type="button"
                    onClick={() => setSelectedDate(date)}
                    aria-pressed={isSelected}
                    className={[
                      'flex aspect-square flex-col items-center justify-center gap-1 rounded-lg text-sm transition',
                      isSelected
                        ? 'bg-surface-hi font-semibold text-ink ring-1 ring-brand-green/40'
                        : 'text-slate-300 hover:bg-surface-hi',
                    ].join(' ')}
                  >
                    <span
                      className={[
                        'tabular-nums',
                        // Hoje ganha só um sublinhado de cor, não um preenchimento.
                        isToday && !isSelected ? 'text-brand-green' : '',
                      ].join(' ')}
                    >
                      {dayNumber}
                    </span>

                    {/* Altura reservada mesmo sem pontos, para as células não
                        pularem de tamanho entre dias com e sem itens. */}
                    <span className="flex h-1.5 items-center gap-0.5">
                      {hasTask && (
                        <span className={`size-1.5 rounded-full ${DOT_COLORS.task}`} />
                      )}
                      {hasWorkout && (
                        <span className={`size-1.5 rounded-full ${DOT_COLORS.workout}`} />
                      )}
                      {hasEntry && (
                        <span className={`size-1.5 rounded-full ${DOT_COLORS.finance}`} />
                      )}
                    </span>
                  </button>
                )
              })}
            </div>

            {/* Legenda: sem ela os pontos são código sem chave. */}
            <div className="mt-3 flex items-center justify-center gap-4 border-t border-line pt-3 text-[10px] text-slate-500">
              <span className="flex items-center gap-1.5">
                <span className={`size-1.5 rounded-full ${DOT_COLORS.task}`} />
                Tarefa
              </span>
              <span className="flex items-center gap-1.5">
                <span className={`size-1.5 rounded-full ${DOT_COLORS.workout}`} />
                Treino
              </span>
              <span className="flex items-center gap-1.5">
                <span className={`size-1.5 rounded-full ${DOT_COLORS.finance}`} />
                Financeiro
              </span>
            </div>
          </div>

          {/* DIA SELECIONADO */}
          <section className="flex flex-col gap-3">
            <SectionHeader
              trailing={
                <button
                  type="button"
                  onClick={() => setAddMenuOpen(true)}
                  aria-label="Adicionar item neste dia"
                  className="rounded-lg p-1 text-slate-400 transition hover:bg-surface hover:text-brand-green"
                >
                  <Plus size={16} />
                </button>
              }
            >
              {formatDayLabel(selectedDate)}
            </SectionHeader>

            {hasItems ? (
              <div className="flex flex-col gap-3">
                {dayTasks.length > 0 && (
                  <div className="flex flex-col gap-1.5">
                    <p className={`text-[11px] font-medium ${TEXT_COLORS.task}`}>
                      Tarefas
                    </p>
                    <div className="divide-y divide-line overflow-hidden rounded-md bg-surface ring-1 ring-line">
                    {dayTasks.map((task) => (
                      <div
                        key={task.id}
                        className="flex items-center gap-2.5 px-4 py-3"
                      >
                        <span className={`size-1.5 shrink-0 rounded-full ${DOT_COLORS.task}`} />
                        <span
                          className={[
                            'min-w-0 flex-1 truncate text-sm',
                            task.completed
                              ? 'text-slate-500 line-through'
                              : 'text-slate-200',
                          ].join(' ')}
                        >
                          {task.title}
                        </span>
                        <span className="shrink-0 text-xs text-slate-500">
                          {task.priority}
                        </span>
                      </div>
                    ))}
                    </div>
                  </div>
                )}

                {dayWorkouts.length > 0 && (
                  <div className="flex flex-col gap-1.5">
                    <p className={`text-[11px] font-medium ${TEXT_COLORS.workout}`}>
                      Treinos
                    </p>
                    <div className="divide-y divide-line overflow-hidden rounded-md bg-surface ring-1 ring-line">
                    {dayWorkouts.map((workout) => (
                      <div
                        key={workout.id}
                        className="flex items-center gap-2.5 px-4 py-3"
                      >
                        <span className={`size-1.5 shrink-0 rounded-full ${DOT_COLORS.workout}`} />
                        <span className="min-w-0 flex-1 truncate text-sm text-slate-200">
                          {workout.type === 'Academia'
                            ? (workout.exerciseName ?? 'Academia')
                            : 'Corrida'}
                        </span>
                        <span className="shrink-0 text-xs text-slate-500">
                          {workout.type === 'Academia'
                            ? workout.loadKg !== null
                              ? `${formatNumber(workout.loadKg)} kg`
                              : workout.type
                            : workout.durationMinutes !== null
                              ? `${workout.durationMinutes} min`
                              : workout.type}
                        </span>
                      </div>
                    ))}
                    </div>
                  </div>
                )}

                {dayEntries.length > 0 && (
                  <div className="flex flex-col gap-1.5">
                    <p className={`text-[11px] font-medium ${TEXT_COLORS.finance}`}>
                      Financeiro
                    </p>
                    <div className="divide-y divide-line overflow-hidden rounded-md bg-surface ring-1 ring-line">
                    {dayEntries.map((entry) => (
                      <div
                        key={entry.id}
                        className="flex items-center gap-2.5 px-4 py-3"
                      >
                        <span className={`size-1.5 shrink-0 rounded-full ${DOT_COLORS.finance}`} />
                        <span className="min-w-0 flex-1 truncate text-sm text-slate-200">
                          {categoryNameOf(entry)}
                        </span>
                        <span
                          className={[
                            'shrink-0 text-xs font-medium tabular-nums',
                            entry.type === 'Entrada'
                              ? 'text-brand-green'
                              : 'text-red-400',
                          ].join(' ')}
                        >
                          {entry.type === 'Entrada' ? '+' : '−'}
                          {formatCurrency(entry.amount)}
                        </span>
                      </div>
                    ))}
                    </div>
                  </div>
                )}
              </div>
            ) : (
              <div className="rounded-md bg-surface px-5 py-8 text-center ring-1 ring-line">
                <p className="text-sm text-slate-400">
                  Nada registrado neste dia.
                </p>
              </div>
            )}
          </section>
        </>
      )}

      {error && (
        <p
          role="alert"
          className="rounded-lg bg-red-500/10 px-3 py-2 text-center text-sm text-red-300 ring-1 ring-red-500/20"
        >
          {error}
        </p>
      )}

      {/*
        MENU DE ADIÇÃO
        Navega para a tela do módulo com ?data=, em vez de repetir os três
        formulários aqui: cada tela já sabe validar e salvar o seu tipo, e
        duplicar isso criaria dois lugares para corrigir a mesma regra.
      */}
      {addMenuOpen && (
        <>
          <button
            type="button"
            aria-label="Fechar menu"
            onClick={() => setAddMenuOpen(false)}
            className="fixed inset-0 z-40 bg-black/60 backdrop-blur-sm"
          />
          <div
            role="dialog"
            aria-modal="true"
            aria-label="Adicionar item"
            className="fixed inset-x-4 bottom-24 z-50 mx-auto max-w-sm rounded-md bg-surface p-3 ring-1 ring-line"
          >
            <div className="flex items-center justify-between px-2 pb-2">
              <p className="text-[11px] font-semibold tracking-[0.14em] text-slate-500 uppercase">
                Adicionar em {formatDayLabel(selectedDate)}
              </p>
              <button
                type="button"
                onClick={() => setAddMenuOpen(false)}
                aria-label="Fechar"
                className="rounded p-1 text-slate-500 transition hover:text-slate-200"
              >
                <X size={16} />
              </button>
            </div>

            <div className="flex flex-col">
              {[
                { to: '/tarefas', label: 'Nova tarefa', color: DOT_COLORS.task },
                { to: '/treino', label: 'Novo treino', color: DOT_COLORS.workout },
                {
                  to: '/financas',
                  label: 'Novo lançamento',
                  color: DOT_COLORS.finance,
                },
              ].map((option) => (
                <Link
                  key={option.to}
                  to={`${option.to}?data=${selectedDate}`}
                  className="flex items-center gap-3 rounded-lg px-3 py-3 text-sm text-slate-200 transition hover:bg-surface-hi"
                >
                  <span className={`size-1.5 rounded-full ${option.color}`} />
                  {option.label}
                </Link>
              ))}
            </div>
          </div>
        </>
      )}
    </div>
  )
}

export default Agenda
