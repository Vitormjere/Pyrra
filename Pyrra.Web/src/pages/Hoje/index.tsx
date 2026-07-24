import { useCallback, useEffect, useRef, useState } from 'react'
import type { FormEvent, ReactNode } from 'react'
import { Link } from 'react-router-dom'
import {
  Dumbbell,
  Footprints,
  Eye,
  EyeOff,
  Plus,
  Sparkles,
  TrendingDown,
  TrendingUp,
  Wallet,
} from 'lucide-react'
import CheckCircle from '../../components/CheckCircle'
import MilestoneCelebration from '../../components/MilestoneCelebration'
import PreviewCard from '../../components/PreviewCard'
import ProgressRing from '../../components/ProgressRing'
import ReflectionCard from '../../components/ReflectionCard'
import SectionHeader from '../../components/SectionHeader'
import Segmented from '../../components/Segmented'
import StreakPill from '../../components/StreakPill'
import ItemActions from '../../components/ItemActions'
import {
  createFocus,
  deactivateFocus,
  getDailyScore,
  toggleCheckIn,
  updateFocus,
} from '../../services/focusService'
import {
  acknowledgeMilestones,
  getPendingMilestones,
  getStreakStatus,
} from '../../services/streakService'
import {
  getBalance,
  getWeeklySummary,
} from '../../services/financeService'
import { getWorkoutPlan } from '../../services/workoutService'
import {
  createTask,
  getTasksForDay,
  toggleTaskCompleted,
} from '../../services/taskService'
import { getApiErrorMessage } from '../../services/apiError'
import {
  formatCurrency,
  formatDayLabel,
  formatPlannedExercise,
} from '../../utils/format'
import { todayWeekDay } from '../../types/plan'
import type { DailyScoreResponse } from '../../types/focus'
import type {
  PendingMilestoneResponse,
  StreakStatusResponse,
} from '../../types/streak'
import type {
  BalanceResponse,
  WeeklyFinanceSummaryResponse,
} from '../../types/finance'
import type { WorkoutPlanDayResponse, WorkoutType } from '../../types/workout'
import type { TaskPriority, TaskResponse } from '../../types/task'

// Máscara dos valores financeiros. Constante para as três linhas usarem o
// mesmo texto — se divergissem, o card pareceria ter dois estados de sigilo.
const MASKED = "R$ ••••••"

const WORKOUT_TABS: readonly WorkoutType[] = ["Academia", "Corrida"]

function toPercent(fraction: number): number {
  return Math.round(fraction * 100)
}

// Uma prévia que falha não deve derrubar o dashboard inteiro. Converter a
// rejeição em null deixa o Promise.all resolver e cada card decide o que
// mostrar quando seu dado não veio.
function optional<T>(promise: Promise<T>): Promise<T | null> {
  return promise.catch(() => null)
}

// Pontos de prioridade, iguais aos da tela de Tarefas.
const TASK_PRIORITY_DOTS: Record<TaskPriority, string> = {
  Baixa: 'bg-slate-400',
  Media: 'bg-sky-400',
  Alta: 'bg-amber-400',
  Urgente: 'bg-red-400',
}

// Mesmos rótulos e cores da tela de Tarefas, para o seletor não divergir.
const TASK_PRIORITIES: readonly TaskPriority[] = ['Baixa', 'Media', 'Alta', 'Urgente']

const TASK_PRIORITY_LABELS: Record<TaskPriority, string> = {
  Baixa: 'Baixa',
  Media: 'Média',
  Alta: 'Alta',
  Urgente: 'Urgente',
}

const TASK_PRIORITY_TEXT: Record<TaskPriority, string> = {
  Baixa: 'text-slate-400',
  Media: 'text-sky-400',
  Alta: 'text-amber-400',
  Urgente: 'text-red-400',
}

// Atalho para a tela completa do módulo, no canto do cabeçalho de seção.
function VerTudo({ to }: { to: string }) {
  return (
    <Link
      to={to}
      className="text-xs font-medium text-slate-500 transition hover:text-brand-green"
    >
      Ver tudo
    </Link>
  )
}

function EmptyBlock({ children }: { children: ReactNode }) {
  return (
    <div className="rounded-md bg-surface px-4 py-5 text-center text-sm text-slate-400 ring-1 ring-line">
      {children}
    </div>
  )
}

// (describeWorkout foi removido: a seção de Treino do dashboard agora mostra
// só o plano, sem dados numéricos de execução.)

function LoadingState() {
  return (
    <div className="flex flex-col gap-4" aria-busy="true" aria-label="Carregando">
      <div className="h-28 animate-pulse rounded-md bg-surface" />
      <div className="h-16 animate-pulse rounded-md bg-surface" />
      <div className="h-14 animate-pulse rounded-md bg-surface" />
      <div className="h-14 animate-pulse rounded-md bg-surface" />
    </div>
  )
}

export function Hoje() {
  const [score, setScore] = useState<DailyScoreResponse | null>(null)
  const [workoutPlan, setWorkoutPlan] = useState<
    WorkoutPlanDayResponse[] | null
  >(null)
  const [streak, setStreak] = useState<StreakStatusResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  // Guarda QUAL foco está em voo, não um booleano: só a linha tocada trava,
  // as outras seguem clicáveis.
  const [pendingFocusId, setPendingFocusId] = useState<string | null>(null)
  // Fila de celebrações: exibimos uma por vez e vamos consumindo pela frente.
  const [milestones, setMilestones] = useState<PendingMilestoneResponse[]>([])
  const [acknowledging, setAcknowledging] = useState(false)
  // Qual tarefa está em voo — só a linha tocada trava.
  const [pendingTaskId, setPendingTaskId] = useState<string | null>(null)

  // Prévias dos módulos. null = a chamada falhou; o card mostra indisponível em
  // vez de sumir, para a ausência não parecer "sem dados".
  const [balance, setBalance] = useState<BalanceResponse | null>(null)
  const [tasks, setTasks] = useState<TaskResponse[] | null>(null)
  // Saldo começa oculto: o dashboard abre logo ao entrar no app, muitas vezes em
  // público. Estado só de sessão, não persistido.
  const [balanceVisible, setBalanceVisible] = useState(false)
  // Aba ativa da seção Foco.
  const [focusTab, setFocusTab] = useState<'habitos' | 'tarefas'>('habitos')
  // Aba ativa da seção Treino.
  const [workoutTab, setWorkoutTab] = useState<WorkoutType>('Academia')
  const [summary, setSummary] = useState<WeeklyFinanceSummaryResponse | null>(null)

  // Criação de foco.
  const [formOpen, setFormOpen] = useState(false)
  const [newFocusName, setNewFocusName] = useState('')
  const [creating, setCreating] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)
  // Edição inline de um foco (só o nome; o backend recategoriza e recalcula o
  // peso). E travas de linha em voo (edição salvando / remoção).
  const [editingFocusId, setEditingFocusId] = useState<string | null>(null)
  const [editFocusName, setEditFocusName] = useState('')
  const [savingFocusEdit, setSavingFocusEdit] = useState(false)
  const [focusEditError, setFocusEditError] = useState<string | null>(null)
  const [deletingFocusId, setDeletingFocusId] = useState<string | null>(null)
  // Devolve o cursor ao campo depois de salvar, para o próximo foco ser digitado
  // sem tocar na tela de novo.
  const nameInputRef = useRef<HTMLInputElement>(null)

  // Criação de tarefa, dentro da aba Tarefas.
  const [taskFormOpen, setTaskFormOpen] = useState(false)
  const [newTaskTitle, setNewTaskTitle] = useState('')
  const [newTaskPriority, setNewTaskPriority] = useState<TaskPriority>('Media')
  const [creatingTask, setCreatingTask] = useState(false)
  const [taskError, setTaskError] = useState<string | null>(null)
  const taskInputRef = useRef<HTMLInputElement>(null)

  // Só busca, não mexe em estado: assim o efeito abaixo consegue adiar todo
  // setState para depois do await, como a regra react-hooks/set-state-in-effect
  // exige, e a mesma função serve ao "tentar de novo".
  const fetchDay = useCallback(async () => {
    // Em paralelo: são endpoints independentes, e em série a tela levaria a
    // soma dos dois tempos para aparecer.
    // score e streak são o núcleo: sem eles a tela não tem o que mostrar, então
    // a falha deles derruba o carregamento. As três prévias passam por
    // optional() — cada uma degrada sozinha sem levar o dashboard junto.
    const [scoreData, streakData, balanceData, tasksData, planData, summaryData] =
      await Promise.all([
        getDailyScore(),
        getStreakStatus(),
        optional(getBalance()),
        optional(getTasksForDay()),
        optional(getWorkoutPlan()),
        optional(getWeeklySummary()),
      ])

    // Depois, nunca junto: é o acerto rodado dentro do GET /api/streak que grava
    // os marcos. Em paralelo, um marco criado agora poderia não estar na lista.
    const pending = await getPendingMilestones()

    return { scoreData, streakData, balanceData, tasksData, planData, summaryData, pending }
  }, [])

  useEffect(() => {
    // Evita setState depois que a tela saiu da árvore (StrictMode monta,
    // desmonta e remonta o efeito em desenvolvimento).
    let active = true

    async function run() {
      try {
        const result = await fetchDay()
        if (!active) return
        setScore(result.scoreData)
        setStreak(result.streakData)
        setBalance(result.balanceData)
        setTasks(result.tasksData)
        setWorkoutPlan(result.planData)
        setSummary(result.summaryData)
        setMilestones(result.pending)
      } catch (err) {
        if (!active) return
        setError(
          getApiErrorMessage(err, {}, 'Não foi possível carregar seu dia.'),
        )
      } finally {
        if (active) setLoading(false)
      }
    }

    void run()

    return () => {
      active = false
    }
  }, [fetchDay])

  async function handleRetry() {
    setLoading(true)
    setError(null)

    try {
      const result = await fetchDay()
      setScore(result.scoreData)
      setStreak(result.streakData)
      setBalance(result.balanceData)
      setTasks(result.tasksData)
      setWorkoutPlan(result.planData)
        setSummary(result.summaryData)
      setMilestones(result.pending)
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível carregar seu dia.'))
    } finally {
      setLoading(false)
    }
  }

  async function handleToggle(focusId: string) {
    setPendingFocusId(focusId)
    setError(null)

    try {
      // O check-in devolve o score já recalculado pelo servidor — nada de
      // recomputar pontos aqui.
      const updatedScore = await toggleCheckIn(focusId)
      setScore(updatedScore)

      // O streak precisa de nova consulta: o GET roda o acerto e pode mexer em
      // currentCount e freezes na virada do dia, que o score sozinho não revela.
      setStreak(await getStreakStatus())

      // E só então os marcos, pela mesma razão da carga inicial: quem os cria é
      // o acerto que acabou de rodar.
      setMilestones(await getPendingMilestones())
    } catch (err) {
      setError(
        getApiErrorMessage(err, {}, 'Não foi possível registrar o check-in.'),
      )
    } finally {
      setPendingFocusId(null)
    }
  }

  async function handleCreateFocus(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const name = newFocusName.trim()
    if (!name) return

    setCreating(true)
    setCreateError(null)

    try {
      const created = await createFocus(name)

      // Entra na lista imediatamente, a partir da própria resposta: o foco nasce
      // sempre não concluído, e category/weight já vêm calculados pelo backend.
      setScore((current) =>
        current
          ? {
              ...current,
              focuses: [
                ...current.focuses,
                {
                  focusId: created.id,
                  name: created.name,
                  weight: created.weight,
                  completed: false,
                },
              ],
            }
          : current,
      )

      setNewFocusName('')
      nameInputRef.current?.focus()

      // O foco novo entra em pointsPossible e derruba a porcentagem do dia, então
      // a barra de progresso ficaria mentindo se parássemos aqui. Recalcular a
      // conta no cliente duplicaria o DailyScoreCalculator (pesos e piso de 70%),
      // então buscamos o score de novo — só ele, não a tela inteira.
      //
      // Tolerante de propósito: se esta parte falhar, o foco JÁ foi criado e está
      // na lista; só os totais ficam defasados até a próxima carga. Deixar o erro
      // subir mostraria "não foi possível criar o foco" para algo que deu certo.
      try {
        setScore(await getDailyScore())
      } catch {
        /* mantém o estado local otimista */
      }
    } catch (err) {
      setCreateError(
        getApiErrorMessage(
          err,
          { 409: 'Você já tem um foco com esse nome.' },
          'Não foi possível criar o foco.',
        ),
      )
    } finally {
      setCreating(false)
    }
  }

  function closeFocusForm() {
    setFormOpen(false)
    setNewFocusName('')
    setCreateError(null)
  }

  function startEditFocus(focus: { focusId: string; name: string }) {
    setEditingFocusId(focus.focusId)
    setEditFocusName(focus.name)
    setFocusEditError(null)
  }

  async function handleSaveFocusEdit(focusId: string) {
    const name = editFocusName.trim()
    if (!name) return

    setSavingFocusEdit(true)
    setFocusEditError(null)

    try {
      const updated = await updateFocus(focusId, name)

      // Atualiza nome e peso na lista a partir da resposta (o backend pode ter
      // recategorizado e mudado o peso).
      setScore((current) =>
        current
          ? {
              ...current,
              focuses: current.focuses.map((focus) =>
                focus.focusId === focusId
                  ? { ...focus, name: updated.name, weight: updated.weight }
                  : focus,
              ),
            }
          : current,
      )

      setEditingFocusId(null)

      // Peso alterado muda pointsPossible e a porcentagem do dia; como na criação,
      // rebusca só o score. Tolerante: se falhar, o item já foi salvo e só os
      // totais ficam defasados até a próxima carga.
      try {
        setScore(await getDailyScore())
      } catch {
        /* mantém o estado local otimista */
      }
    } catch (err) {
      setFocusEditError(
        getApiErrorMessage(
          err,
          { 409: 'Você já tem um foco com esse nome.' },
          'Não foi possível salvar o foco.',
        ),
      )
    } finally {
      setSavingFocusEdit(false)
    }
  }

  async function handleDeleteFocus(focus: { focusId: string; name: string }) {
    if (!window.confirm(`Remover o foco "${focus.name}"?`)) return

    setDeletingFocusId(focus.focusId)
    setError(null)

    try {
      await deactivateFocus(focus.focusId)

      // Sai da lista na hora. Se era o que estava em edição, encerra a edição.
      setScore((current) =>
        current
          ? {
              ...current,
              focuses: current.focuses.filter(
                (f) => f.focusId !== focus.focusId,
              ),
            }
          : current,
      )
      if (editingFocusId === focus.focusId) setEditingFocusId(null)

      // Remover um foco muda pointsPossible e a porcentagem: rebusca só o score.
      try {
        setScore(await getDailyScore())
      } catch {
        /* mantém o estado local otimista */
      }
    } catch (err) {
      setError(
        getApiErrorMessage(err, {}, 'Não foi possível remover o foco.'),
      )
    } finally {
      setDeletingFocusId(null)
    }
  }

  // Mesmo comportamento da tela de Tarefas: a resposta do PATCH substitui o
  // item. Aqui nada é derivado (o score do dia é de focos, não de tarefas),
  // então não há o que reconciliar.
  async function handleToggleTask(taskId: string) {
    setPendingTaskId(taskId)
    setError(null)

    try {
      const updated = await toggleTaskCompleted(taskId)
      setTasks((current) =>
        current
          ? current.map((task) => (task.id === updated.id ? updated : task))
          : current,
      )
    } catch (err) {
      setError(
        getApiErrorMessage(err, {}, 'Não foi possível atualizar a tarefa.'),
      )
    } finally {
      setPendingTaskId(null)
    }
  }

  async function handleCreateTask(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const title = newTaskTitle.trim()
    if (!title) return

    setCreatingTask(true)
    setTaskError(null)

    try {
      // Sem data: o backend usa hoje no fuso do usuário, que é exatamente o dia
      // que esta lista mostra.
      const created = await createTask(title, newTaskPriority)
      setTasks((current) => (current ? [...current, created] : [created]))
      setNewTaskTitle('')
      taskInputRef.current?.focus()
    } catch (err) {
      setTaskError(
        getApiErrorMessage(err, {}, 'Não foi possível criar a tarefa.'),
      )
    } finally {
      setCreatingTask(false)
    }
  }

  async function handleAcknowledgeMilestone() {
    const current = milestones[0]
    if (!current) return

    setAcknowledging(true)

    try {
      // Confirma SÓ o marco exibido, em vez de mandar a lista vazia (que o
      // backend interpreta como "confirmar todos"). Se o usuário fechar o app no
      // meio da fila, os que ele ainda não viu continuam pendentes.
      await acknowledgeMilestones([current.id])
    } catch {
      // Falhou a confirmação: o marco segue pendente no servidor e reaparece na
      // próxima carga. Avançar a fila mesmo assim é melhor que prender o usuário
      // num modal que não fecha — o pior caso é ver a celebração duas vezes.
    } finally {
      setMilestones((queue) => queue.slice(1))
      setAcknowledging(false)
    }
  }

  if (loading) {
    return <LoadingState />
  }

  if (error && !score) {
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

  if (!score || !streak) {
    return null
  }

  const percent = toPercent(score.percentage)
  const hasFocuses = score.focuses.length > 0

  // "Hoje" vem do servidor (score.date, já no fuso do usuário) e não do
  // navegador: o dispositivo pode estar em outro fuso — viagem, VPN — e a
  // comparação daria falso negativo bem na virada do dia.
  // getTasksForDay já devolve só as de hoje.
  const todayTasks = tasks ?? []

  // Exercícios planejados para hoje.
  const todayPlanExercises =
    workoutPlan?.find((day) => day.dayOfWeek === todayWeekDay())?.exercises ?? []

  const tabExercises = todayPlanExercises.filter(
    (exercise) => exercise.type === workoutTab,
  )

  // Label do plano para o dia da semana corrente. Vazio ou só espaços conta
  // como "sem plano", igual à regra do backend.
  const todayPlanLabel =
    workoutPlan
      ?.find((day) => day.dayOfWeek === todayWeekDay())
      ?.label?.trim() || null

  return (
    <div className="flex flex-col gap-5">
      {/* Cabeçalho: data à esquerda, streak como pill discreta à direita — o
          foguinho deixou de ser o card dominante da tela. */}
      <header className="flex items-start justify-between gap-3">
        <div>
          <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">Hoje</h1>
          <p className="mt-1 text-sm text-slate-500 first-letter:uppercase">
            {formatDayLabel(score.date)}
          </p>
        </div>
        <div className="pt-1.5">
          <StreakPill
            days={streak.displayCount}
            freezes={streak.freezesAvailable}
          />
        </div>
      </header>

      {/* PROGRESSO DO DIA — anel no lugar da barra: o número no centro vira o
          ponto focal da tela, e o arco só o contextualiza. */}
      {/* Único card que mantém canto bem arredondado: é a peça principal da
          tela e o arredondamento reforça a forma circular do anel. */}
      <section className="flex items-center gap-5 rounded-2xl bg-surface px-5 py-5 ring-1 ring-line">
        <ProgressRing
          percent={percent}
          value={`${percent}%`}
          label="Hoje"
          accent={score.goalMet}
        />

        <div className="min-w-0">
          <SectionHeader>Meta do dia</SectionHeader>
          {score.goalMet ? (
            <p className="glow-text mt-1.5 flex items-center gap-1.5 text-sm font-medium text-brand-green">
              <Sparkles size={14} aria-hidden="true" />
              Meta batida
            </p>
          ) : (
            <p className="mt-1.5 text-sm text-slate-400 tabular-nums">
              {score.pointsEarned} de {score.pointsPossible} pontos
            </p>
          )}
          <p className="mt-1 text-xs text-slate-500">
            {score.focuses.filter((focus) => focus.completed).length} de{' '}
            {score.focuses.length} focos concluídos
          </p>
        </div>
      </section>

      {/*
        FOCO — hábitos e tarefas debaixo do mesmo header, alternados por abas.
        Os dois respondem à mesma pergunta ("o que eu preciso fazer hoje?"), e
        separá-los em duas seções fazia a tela repetir a mesma estrutura duas
        vezes seguidas. As listas e seus comportamentos não mudaram.
      */}
      <section className="flex flex-col gap-2">
        <SectionHeader>Foco</SectionHeader>

        <div role="tablist" className="flex gap-1 rounded-md bg-surface p-1">
          {(
            [
              { key: 'habitos' as const, label: 'Hábitos', count: score.focuses.length },
              { key: 'tarefas' as const, label: 'Tarefas', count: todayTasks.length },
            ]
          ).map((option) => (
            <button
              key={option.key}
              type="button"
              role="tab"
              aria-selected={focusTab === option.key}
              onClick={() => setFocusTab(option.key)}
              className={[
                'flex flex-1 items-center justify-center gap-2 rounded-lg px-3 py-2 text-sm font-medium transition',
                focusTab === option.key
                  ? 'bg-surface-hi text-ink'
                  : 'text-slate-400 hover:text-slate-200',
              ].join(' ')}
            >
              {option.label}
              {option.count > 0 && (
                <span className="rounded-full bg-surface px-1.5 py-0.5 text-[10px] font-semibold tabular-nums">
                  {option.count}
                </span>
              )}
            </button>
          ))}
        </div>

        {focusTab === 'habitos' ? (
          <>
            {hasFocuses ? (
          <ul className="divide-y divide-line overflow-hidden rounded-md bg-surface ring-1 ring-line">
            {score.focuses.map((focus) => {
              const pending = pendingFocusId === focus.focusId
              const deleting = deletingFocusId === focus.focusId

              if (editingFocusId === focus.focusId) {
                return (
                  <li key={focus.focusId}>
                    <form
                      onSubmit={(event) => {
                        event.preventDefault()
                        void handleSaveFocusEdit(focus.focusId)
                      }}
                      className="flex flex-col gap-2 bg-surface-hi px-4 py-3.5"
                    >
                      <label
                        htmlFor={`editar-foco-${focus.focusId}`}
                        className="sr-only"
                      >
                        Nome do foco
                      </label>
                      <input
                        id={`editar-foco-${focus.focusId}`}
                        type="text"
                        value={editFocusName}
                        onChange={(event) => {
                          setEditFocusName(event.target.value)
                          if (focusEditError !== null) setFocusEditError(null)
                        }}
                        autoFocus
                        maxLength={100}
                        className="w-full rounded-md bg-surface px-4 py-3 text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green"
                      />

                      <div className="flex gap-2">
                        <button
                          type="submit"
                          disabled={
                            savingFocusEdit || editFocusName.trim().length === 0
                          }
                          className="flex-1 rounded-xl bg-brand-green px-4 py-2.5 font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
                        >
                          {savingFocusEdit ? 'Salvando...' : 'Salvar'}
                        </button>
                        <button
                          type="button"
                          onClick={() => setEditingFocusId(null)}
                          className="rounded-md px-4 py-2.5 font-medium text-slate-400 transition hover:bg-surface hover:text-slate-200"
                        >
                          Cancelar
                        </button>
                      </div>

                      {focusEditError && (
                        <p role="alert" className="text-sm text-red-300">
                          {focusEditError}
                        </p>
                      )}
                    </form>
                  </li>
                )
              }

              return (
                <li key={focus.focusId}>
                  {/*
                    A linha deixou de ser um único <button>: com ações irmãs
                    (editar/remover), botão dentro de botão seria HTML inválido.
                    O toggle cobre o conteúdo; as ações ficam ao lado.
                  */}
                  <div className="flex items-center gap-1 pr-2 transition hover:bg-surface-hi">
                    <button
                      type="button"
                      role="checkbox"
                      aria-checked={focus.completed}
                      disabled={pending || deleting}
                      onClick={() => handleToggle(focus.focusId)}
                      className="flex flex-1 items-center gap-3 px-4 py-3.5 text-left disabled:opacity-50"
                    >
                      <CheckCircle checked={focus.completed} />

                      <span
                        className={[
                          'min-w-0 flex-1 truncate transition',
                          focus.completed
                            ? 'text-slate-400 line-through'
                            : 'text-ink',
                        ].join(' ')}
                      >
                        {focus.name}
                      </span>

                      <span className="shrink-0 text-xs text-slate-500 tabular-nums">
                        peso {focus.weight}
                      </span>
                    </button>

                    <ItemActions
                      busy={pending || deleting}
                      onEdit={() => startEditFocus(focus)}
                      onDelete={() => handleDeleteFocus(focus)}
                      editLabel={`Editar ${focus.name}`}
                      deleteLabel={`Remover ${focus.name}`}
                    />
                  </div>
                </li>
              )
            })}
          </ul>
        ) : (
          <div className="rounded-md bg-surface px-5 py-8 text-center ring-1 ring-line">
            <p className="font-medium text-slate-200">
              Você ainda não tem focos.
            </p>
            <p className="mt-1.5 text-sm text-slate-400">
              Focos são os hábitos que você quer seguir todo dia. Crie o
              primeiro para o seu foguinho começar a contar.
            </p>
          </div>
        )}

        {/*
          O formulário PERMANECE aberto após salvar, com o campo limpo e o cursor
          de volta nele. Quem chega aqui costuma cadastrar vários focos de uma vez
          (é literalmente o passo de onboarding), e fechar a cada item obrigaria a
          reabrir a cada novo foco. Sair é explícito, pelo botão Fechar.
        */}
        {formOpen ? (
          <form
            onSubmit={handleCreateFocus}
            className="flex flex-col gap-2 rounded-md bg-surface p-3 ring-1 ring-line"
          >
            <label htmlFor="novo-foco" className="sr-only">
              Nome do foco
            </label>
            <input
              id="novo-foco"
              ref={nameInputRef}
              type="text"
              value={newFocusName}
              onChange={(event) => {
                setNewFocusName(event.target.value)
                if (createError !== null) setCreateError(null)
              }}
              autoFocus
              maxLength={100}
              placeholder="Ex: beber água"
              className="w-full rounded-md bg-surface px-4 py-3 text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green"
            />

            <div className="flex gap-2">
              <button
                type="submit"
                // Campo vazio não chega a virar requisição.
                disabled={creating || newFocusName.trim().length === 0}
                className="flex-1 rounded-xl bg-brand-green px-4 py-2.5 font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {creating ? 'Salvando...' : 'Adicionar'}
              </button>
              <button
                type="button"
                onClick={closeFocusForm}
                className="rounded-md px-4 py-2.5 font-medium text-slate-400 transition hover:bg-surface hover:text-slate-200"
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
          // Borda tracejada: lê como "espaço a preencher", diferente dos cards de
          // conteúdo, e não compete com o verde do botão primário.
          <button
            type="button"
            onClick={() => setFormOpen(true)}
            className="flex min-h-12 w-full items-center justify-center gap-2 rounded-md border-2 border-dashed border-line text-sm font-medium text-slate-400 transition hover:border-slate-600 hover:bg-surface hover:text-slate-200"
          >
            <Plus size={18} aria-hidden="true" />
            Adicionar foco
          </button>
        )}
          </>
        ) : (
          // ABA TAREFAS — mesma lista e mesmo toggle de antes, agora aqui dentro.
          <>
            {tasks === null ? (
              <EmptyBlock>Tarefas indisponíveis agora.</EmptyBlock>
            ) : todayTasks.length > 0 ? (
              <ul className="divide-y divide-line overflow-hidden rounded-md bg-surface ring-1 ring-line">
                {todayTasks.map((task) => (
                  <li key={task.id}>
                    <button
                      type="button"
                      role="checkbox"
                      aria-checked={task.completed}
                      disabled={pendingTaskId === task.id}
                      onClick={() => handleToggleTask(task.id)}
                      className="flex w-full items-center gap-3 px-4 py-3.5 text-left transition hover:bg-surface-hi disabled:opacity-50"
                    >
                      <CheckCircle checked={task.completed} />
                      <span
                        className={[
                          'min-w-0 flex-1 truncate transition',
                          task.completed
                            ? 'text-slate-400 line-through'
                            : 'text-ink',
                        ].join(' ')}
                      >
                        {task.title}
                      </span>
                      <span
                        aria-hidden="true"
                        className={`size-1.5 shrink-0 rounded-full ${TASK_PRIORITY_DOTS[task.priority]}`}
                      />
                    </button>
                  </li>
                ))}
              </ul>
            ) : (
              <EmptyBlock>Nenhuma tarefa para hoje.</EmptyBlock>
            )}

            {/* Mesmo padrão do formulário de foco: permanece aberto após criar,
                com prioridade preservada, porque quem lista tarefas costuma
                lançar várias seguidas. */}
            {taskFormOpen ? (
              <form
                onSubmit={handleCreateTask}
                className="flex flex-col gap-2 rounded-md bg-surface p-3 ring-1 ring-line"
              >
                <label htmlFor="nova-tarefa-hoje" className="sr-only">
                  Título da tarefa
                </label>
                <input
                  id="nova-tarefa-hoje"
                  ref={taskInputRef}
                  type="text"
                  value={newTaskTitle}
                  onChange={(event) => {
                    setNewTaskTitle(event.target.value)
                    if (taskError !== null) setTaskError(null)
                  }}
                  autoFocus
                  maxLength={500}
                  placeholder="Ex: pagar a conta de luz"
                  className="w-full rounded-md bg-surface px-4 py-3 text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green"
                />

                <Segmented
                  label="Prioridade"
                  options={TASK_PRIORITIES}
                  value={newTaskPriority}
                  onChange={setNewTaskPriority}
                  labels={TASK_PRIORITY_LABELS}
                  activeColors={TASK_PRIORITY_TEXT}
                />

                <div className="flex gap-2">
                  <button
                    type="submit"
                    disabled={creatingTask || newTaskTitle.trim().length === 0}
                    className="flex-1 rounded-xl bg-brand-green px-4 py-2.5 font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    {creatingTask ? 'Salvando...' : 'Adicionar'}
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      setTaskFormOpen(false)
                      setNewTaskTitle('')
                      setTaskError(null)
                    }}
                    className="rounded-md px-4 py-2.5 font-medium text-slate-400 transition hover:bg-surface hover:text-slate-200"
                  >
                    Fechar
                  </button>
                </div>

                {taskError && (
                  <p role="alert" className="text-sm text-red-300">
                    {taskError}
                  </p>
                )}
              </form>
            ) : (
              <button
                type="button"
                onClick={() => setTaskFormOpen(true)}
                className="flex min-h-12 w-full items-center justify-center gap-2 rounded-md border-2 border-dashed border-line text-sm font-medium text-slate-400 transition hover:border-slate-600 hover:bg-surface hover:text-slate-200"
              >
                <Plus size={18} aria-hidden="true" />
                Adicionar tarefa
              </button>
            )}

            <VerTudo to="/tarefas" />
          </>
        )}
      </section>

      <ReflectionCard />

      {/*
        TREINO — agora só o PLANEJADO para hoje, não o histórico real. O que foi
        de fato registrado continua na tela Treino, a um toque do "Ver tudo": o
        dashboard responde "o que fazer", não "o que já fiz".
      */}
      <section className="flex flex-col gap-2">
        <SectionHeader trailing={<VerTudo to="/treino" />}>Treino</SectionHeader>

        {todayPlanLabel && (
          <p className="text-sm text-slate-500">{todayPlanLabel}</p>
        )}

        {/* Abas por modalidade, mesmo padrão da seção Foco. */}
        <div role="tablist" className="flex gap-1 rounded-md bg-surface p-1">
          {WORKOUT_TABS.map((option) => {
            const count = todayPlanExercises.filter(
              (exercise) => exercise.type === option,
            ).length

            return (
              <button
                key={option}
                type="button"
                role="tab"
                aria-selected={workoutTab === option}
                onClick={() => setWorkoutTab(option)}
                className={[
                  'flex flex-1 items-center justify-center gap-2 rounded-lg px-3 py-2 text-sm font-medium transition',
                  workoutTab === option
                    ? 'bg-surface-hi text-ink'
                    : 'text-slate-400 hover:text-slate-200',
                ].join(' ')}
              >
                {option}
                {count > 0 && (
                  <span className="rounded-full bg-surface px-1.5 py-0.5 text-[10px] font-semibold tabular-nums">
                    {count}
                  </span>
                )}
              </button>
            )
          })}
        </div>

        {tabExercises.length > 0 ? (
          <ul className="divide-y divide-line overflow-hidden rounded-md bg-surface ring-1 ring-line">
            {tabExercises.map((exercise) => (
              <li
                key={exercise.id}
                className="flex items-center gap-3 px-4 py-3.5"
              >
                {workoutTab === 'Academia' ? (
                  <Dumbbell
                    size={16}
                    className="shrink-0 text-slate-500"
                    aria-hidden="true"
                  />
                ) : (
                  <Footprints
                    size={16}
                    className="shrink-0 text-slate-500"
                    aria-hidden="true"
                  />
                )}
                <span className="min-w-0 flex-1 truncate text-ink">
                  {formatPlannedExercise(
                    exercise.exerciseName,
                    exercise.sets,
                    exercise.reps,
                  )}
                </span>
              </li>
            ))}
          </ul>
        ) : (
          <EmptyBlock>
            {workoutTab === 'Academia'
              ? 'Nenhuma academia planejada para hoje.'
              : 'Nenhuma corrida planejada para hoje.'}
          </EmptyBlock>
        )}
      </section>

      {/* FINANÇAS — sem o header genérico "Resumo": o card já se identifica. */}
      <PreviewCard
        to="/financas"
        title="Finanças"
        icon={Wallet}
        action={
          <button
            type="button"
            onClick={() => setBalanceVisible((visible) => !visible)}
            aria-pressed={balanceVisible}
            aria-label={balanceVisible ? 'Ocultar valores' : 'Mostrar valores'}
            className="rounded-lg p-1.5 text-slate-400 transition hover:bg-surface-hi hover:text-slate-200"
          >
            {balanceVisible ? <Eye size={16} /> : <EyeOff size={16} />}
          </button>
        }
      >
        {balance ? (
          <>
            {/* Saldo bem maior que as linhas de fluxo: a hierarquia de tamanho
                é o que diz, sem rótulo, qual número é o principal. */}
            <p className="glow-ink text-4xl font-semibold text-ink tabular-nums">
              {balanceVisible ? formatCurrency(balance.currentBalance) : MASKED}
            </p>

            {/* Entradas e saídas DO PERÍODO (semana), não acumuladas — por isso
                vêm do resumo semanal e não do saldo. O mesmo olho governa as
                três linhas: esconder só o saldo deixaria o resto a descoberto.

                Ícone colado ao valor (gap-1), sem rótulo: a seta de tendência já
                diz entrada ou saída, e "Entradas"/"Saídas" seria redundância. */}
            {summary && (
              <div className="mt-2 flex items-center gap-4 text-sm">
                <span className="flex items-center gap-1">
                  <TrendingUp
                    size={15}
                    className="shrink-0 text-brand-green"
                    aria-hidden="true"
                  />
                  <span className="sr-only">Entradas da semana:</span>
                  <span className="text-slate-300 tabular-nums">
                    {balanceVisible
                      ? formatCurrency(summary.periodTotalIn)
                      : MASKED}
                  </span>
                </span>

                <span className="flex items-center gap-1">
                  <TrendingDown
                    size={15}
                    className="shrink-0 text-red-400"
                    aria-hidden="true"
                  />
                  <span className="sr-only">Saídas da semana:</span>
                  <span className="text-slate-300 tabular-nums">
                    {balanceVisible
                      ? formatCurrency(summary.periodTotalOut)
                      : MASKED}
                  </span>
                </span>
              </div>
            )}
          </>
        ) : (
          <p className="text-sm text-slate-500">Saldo indisponível agora.</p>
        )}
      </PreviewCard>

      {/* Falha de check-in: o conteúdo continua na tela, só o aviso aparece. */}
      {error && score && (
        <p
          role="alert"
          className="rounded-lg bg-red-500/10 px-3 py-2 text-center text-sm text-red-300 ring-1 ring-red-500/20"
        >
          {error}
        </p>
      )}

      {/* Um marco por vez: o primeiro da fila. Ao confirmar, ele sai e o
          próximo assume, sem a tela precisar reabrir nada. */}
      {milestones.length > 0 && (
        <MilestoneCelebration
          milestone={milestones[0]}
          remaining={milestones.length - 1}
          submitting={acknowledging}
          onConfirm={handleAcknowledgeMilestone}
        />
      )}
    </div>
  )
}

export default Hoje
