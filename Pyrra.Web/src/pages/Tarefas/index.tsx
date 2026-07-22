import { useCallback, useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { CalendarDays, Check, Plus } from 'lucide-react'
import {
  createTask,
  getPendingTasksForWeek,
  getTasksForDay,
  toggleTaskCompleted,
} from '../../services/taskService'
import { getApiErrorMessage } from '../../services/apiError'
import { formatShortDate, todayIso } from '../../utils/format'
import type { TaskPriority, TaskResponse } from '../../types/task'

const PRIORITIES: TaskPriority[] = ['Baixa', 'Media', 'Alta', 'Urgente']

// Rótulo com acento, separado do valor que trafega na API.
const PRIORITY_LABELS: Record<TaskPriority, string> = {
  Baixa: 'Baixa',
  Media: 'Média',
  Alta: 'Alta',
  Urgente: 'Urgente',
}

// Cores semânticas de urgência — frias para o que pode esperar, quentes para o
// que não pode. Nenhuma usa brand-green de propósito: o verde continua reservado
// a ação e conquista, e prioridade alta não é nem uma coisa nem outra.
const PRIORITY_COLORS: Record<TaskPriority, string> = {
  Baixa: 'text-slate-400',
  Media: 'text-sky-400',
  Alta: 'text-amber-400',
  Urgente: 'text-red-400',
}

const PRIORITY_DOTS: Record<TaskPriority, string> = {
  Baixa: 'bg-slate-400',
  Media: 'bg-sky-400',
  Alta: 'bg-amber-400',
  Urgente: 'bg-red-400',
}

// Espelha a ordenação do PriorityTaskRepository.GetByUserAndDateAsync:
// prioridade DESC e, dentro da mesma faixa, CreatedAt ASCENDENTE — ou seja, na
// ordem em que o usuário escreveu, não do mais recente para o mais antigo.
// Reproduzir a regra aqui é o que permite inserir uma tarefa nova na posição
// certa sem refazer a consulta.
const PRIORITY_RANK: Record<TaskPriority, number> = {
  Baixa: 0,
  Media: 1,
  Alta: 2,
  Urgente: 3,
}

function compareTasks(a: TaskResponse, b: TaskResponse): number {
  const byPriority = PRIORITY_RANK[b.priority] - PRIORITY_RANK[a.priority]
  if (byPriority !== 0) return byPriority
  // createdAt é ISO 8601, então a comparação textual já respeita a cronologia.
  return a.createdAt.localeCompare(b.createdAt)
}

const inputClasses =
  'w-full rounded-xl bg-white/5 px-4 py-3 text-slate-100 ring-1 ring-white/10 transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green'

interface TaskRowProps {
  task: TaskResponse
  pending: boolean
  /** Data curta à direita — usada só na lista de atrasadas. */
  showDate?: boolean
  onToggle: (taskId: string) => void
}

function TaskRow({ task, pending, showDate = false, onToggle }: TaskRowProps) {
  return (
    // A linha inteira é o alvo: em mobile, acertar a faixa toda é bem mais
    // fácil que mirar um quadradinho de 24px.
    <button
      type="button"
      role="checkbox"
      aria-checked={task.completed}
      disabled={pending}
      onClick={() => onToggle(task.id)}
      className="flex w-full items-center gap-3 rounded-xl bg-white/5 px-4 py-3.5 text-left ring-1 ring-white/10 transition hover:bg-white/10 disabled:opacity-50"
    >
      <span
        aria-hidden="true"
        className={[
          'flex size-6 shrink-0 items-center justify-center rounded-md border-2 transition',
          task.completed
            ? 'border-brand-green bg-brand-green text-brand-dark'
            : 'border-white/25',
        ].join(' ')}
      >
        {task.completed && <Check size={16} strokeWidth={3} />}
      </span>

      <span className="flex-1">
        <span
          className={[
            'block transition',
            task.completed ? 'text-slate-400 line-through' : 'text-slate-100',
          ].join(' ')}
        >
          {task.title}
        </span>
        <span className="mt-1 flex items-center gap-1.5">
          <span
            aria-hidden="true"
            className={`size-1.5 rounded-full ${PRIORITY_DOTS[task.priority]}`}
          />
          <span className={`text-xs ${PRIORITY_COLORS[task.priority]}`}>
            {PRIORITY_LABELS[task.priority]}
          </span>
        </span>
      </span>

      {showDate && (
        <span className="shrink-0 text-xs text-slate-500 tabular-nums">
          {formatShortDate(task.date)}
        </span>
      )}
    </button>
  )
}

function LoadingState() {
  return (
    <div className="flex flex-col gap-3" aria-busy="true" aria-label="Carregando">
      <div className="h-10 animate-pulse rounded-xl bg-white/5" />
      <div className="h-16 animate-pulse rounded-xl bg-white/5" />
      <div className="h-16 animate-pulse rounded-xl bg-white/5" />
      <div className="h-16 animate-pulse rounded-xl bg-white/5" />
    </div>
  )
}

type Tab = 'hoje' | 'atrasadas'

export function Tarefas() {
  const [todayTasks, setTodayTasks] = useState<TaskResponse[]>([])
  const [overdueTasks, setOverdueTasks] = useState<TaskResponse[]>([])
  // Segunda-feira da semana consultada, vinda do backend — é ele quem normaliza
  // a data para o início da semana no fuso do usuário.
  const [weekStart, setWeekStart] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [tab, setTab] = useState<Tab>('hoje')
  const [pendingTaskId, setPendingTaskId] = useState<string | null>(null)

  const [formOpen, setFormOpen] = useState(false)
  const [title, setTitle] = useState('')
  const [priority, setPriority] = useState<TaskPriority>('Media')
  const [creating, setCreating] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)
  const titleInputRef = useRef<HTMLInputElement>(null)

  // Só busca, não mexe em estado: mantém todo setState depois do await, como a
  // regra react-hooks/set-state-in-effect exige.
  const fetchTasks = useCallback(async () => {
    const [today, week] = await Promise.all([
      getTasksForDay(),
      getPendingTasksForWeek(),
    ])
    return { today, overdue: week.tasks, weekStart: week.weekStart }
  }, [])

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const result = await fetchTasks()
        if (!active) return
        setTodayTasks(result.today)
        setOverdueTasks(result.overdue)
        setWeekStart(result.weekStart)
      } catch (err) {
        if (!active) return
        setError(
          getApiErrorMessage(err, {}, 'Não foi possível carregar suas tarefas.'),
        )
      } finally {
        if (active) setLoading(false)
      }
    }

    void run()

    return () => {
      active = false
    }
  }, [fetchTasks])

  async function handleRetry() {
    setLoading(true)
    setError(null)

    try {
      const result = await fetchTasks()
      setTodayTasks(result.today)
      setOverdueTasks(result.overdue)
      setWeekStart(result.weekStart)
    } catch (err) {
      setError(
        getApiErrorMessage(err, {}, 'Não foi possível carregar suas tarefas.'),
      )
    } finally {
      setLoading(false)
    }
  }

  async function handleToggle(taskId: string) {
    setPendingTaskId(taskId)
    setError(null)

    try {
      const updated = await toggleTaskCompleted(taskId)

      // Troca só o item afetado nas duas listas. Diferente do check-in de foco,
      // nada aqui é derivado (não há score nem meta), então a resposta do PATCH
      // basta — sem segunda consulta.
      const replace = (tasks: TaskResponse[]) =>
        tasks.map((task) => (task.id === updated.id ? updated : task))

      setTodayTasks(replace)
      // A atrasada concluída CONTINUA na lista, riscada, até a próxima carga:
      // some-la na hora tornaria impossível desfazer um toque errado, e o
      // servidor já não a devolve no próximo GET.
      setOverdueTasks(replace)
    } catch (err) {
      setError(
        getApiErrorMessage(err, {}, 'Não foi possível atualizar a tarefa.'),
      )
    } finally {
      setPendingTaskId(null)
    }
  }

  async function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const trimmedTitle = title.trim()
    if (!trimmedTitle) return

    setCreating(true)
    setCreateError(null)

    try {
      const created = await createTask(trimmedTitle, priority)

      // Insere na posição correta em vez de anexar no fim: com prioridade DESC,
      // uma tarefa Urgente criada agora pertence ao topo, não ao rodapé.
      setTodayTasks((tasks) => [...tasks, created].sort(compareTasks))

      setTitle('')
      // Prioridade permanece escolhida: quem cadastra várias de uma vez costuma
      // repetir a faixa, e reescolher a cada item seria atrito à toa.
      titleInputRef.current?.focus()
    } catch (err) {
      setCreateError(
        getApiErrorMessage(err, {}, 'Não foi possível criar a tarefa.'),
      )
    } finally {
      setCreating(false)
    }
  }

  function closeForm() {
    setFormOpen(false)
    setTitle('')
    setCreateError(null)
  }

  if (loading) {
    return <LoadingState />
  }

  if (error && todayTasks.length === 0 && overdueTasks.length === 0) {
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

  const overdueCount = overdueTasks.filter((task) => !task.completed).length

  // Na segunda-feira a lista vem vazia porque a semana ainda não tem dia
  // passado, não porque o usuário deu conta de tudo — a mensagem de elogio
  // seria falsa. Comparar com weekStart expressa exatamente essa causa: hoje É
  // o primeiro dia da semana. O limite da semana vem do backend; só "hoje" sai
  // do navegador, e num fuso divergente o pior caso é exibir a outra mensagem.
  const isFirstDayOfWeek = weekStart !== null && weekStart === todayIso()

  return (
    <div className="flex flex-col gap-5">
      <header>
        <h1 className="font-display text-3xl tracking-tight">Tarefas</h1>
      </header>

      {/*
        Abas em vez de duas listas empilhadas: a tela fica curta no celular. O
        contador na aba é o que evita o risco do formato — atrasada escondida é
        atrasada esquecida, então o número aparece mesmo sem trocar de aba.
      */}
      <div role="tablist" className="flex gap-1 rounded-xl bg-white/5 p-1">
        <button
          type="button"
          role="tab"
          aria-selected={tab === 'hoje'}
          onClick={() => setTab('hoje')}
          className={[
            'flex-1 rounded-lg px-3 py-2 text-sm font-medium transition',
            tab === 'hoje'
              ? 'bg-white/10 text-slate-100'
              : 'text-slate-400 hover:text-slate-200',
          ].join(' ')}
        >
          Hoje
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={tab === 'atrasadas'}
          onClick={() => setTab('atrasadas')}
          className={[
            'flex flex-1 items-center justify-center gap-2 rounded-lg px-3 py-2 text-sm font-medium transition',
            tab === 'atrasadas'
              ? 'bg-white/10 text-slate-100'
              : 'text-slate-400 hover:text-slate-200',
          ].join(' ')}
        >
          Atrasadas
          {overdueCount > 0 && (
            <span className="rounded-full bg-amber-400/15 px-2 py-0.5 text-xs font-semibold text-amber-400 tabular-nums">
              {overdueCount}
            </span>
          )}
        </button>
      </div>

      {tab === 'hoje' ? (
        <section className="flex flex-col gap-3">
          {todayTasks.length > 0 ? (
            <ul className="flex flex-col gap-2">
              {todayTasks.map((task) => (
                <li key={task.id}>
                  <TaskRow
                    task={task}
                    pending={pendingTaskId === task.id}
                    onToggle={handleToggle}
                  />
                </li>
              ))}
            </ul>
          ) : (
            <div className="rounded-2xl bg-white/5 px-5 py-8 text-center ring-1 ring-white/10">
              <p className="font-medium text-slate-200">
                Nenhuma tarefa para hoje.
              </p>
              <p className="mt-1.5 text-sm text-slate-400">
                Anote o que precisa sair do papel hoje e marque conforme for
                concluindo.
              </p>
            </div>
          )}

          {/* Mesmo padrão de Focos: o formulário fica aberto após salvar, porque
              tarefa raramente se cadastra uma só. */}
          {formOpen ? (
            <form
              onSubmit={handleCreate}
              className="flex flex-col gap-3 rounded-xl bg-white/5 p-3 ring-1 ring-white/10"
            >
              <label htmlFor="nova-tarefa" className="sr-only">
                Título da tarefa
              </label>
              <input
                id="nova-tarefa"
                ref={titleInputRef}
                type="text"
                value={title}
                onChange={(event) => {
                  setTitle(event.target.value)
                  if (createError !== null) setCreateError(null)
                }}
                autoFocus
                maxLength={500}
                placeholder="Ex: pagar a conta de luz"
                className={inputClasses}
              />

              {/* Segmentado, não dropdown: escolher prioridade vira um toque só,
                  contra dois (abrir + selecionar) no select nativo. */}
              <fieldset>
                <legend className="sr-only">Prioridade</legend>
                <div className="flex gap-1 rounded-xl bg-white/5 p-1">
                  {PRIORITIES.map((option) => (
                    <button
                      key={option}
                      type="button"
                      aria-pressed={priority === option}
                      onClick={() => setPriority(option)}
                      className={[
                        'flex-1 rounded-lg px-2 py-2 text-xs font-medium transition',
                        priority === option
                          ? `bg-white/10 ${PRIORITY_COLORS[option]}`
                          : 'text-slate-400 hover:text-slate-200',
                      ].join(' ')}
                    >
                      {PRIORITY_LABELS[option]}
                    </button>
                  ))}
                </div>
              </fieldset>

              <div className="flex gap-2">
                <button
                  type="submit"
                  disabled={creating || title.trim().length === 0}
                  className="flex-1 rounded-xl bg-brand-green px-4 py-2.5 font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {creating ? 'Salvando...' : 'Adicionar'}
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
              Adicionar tarefa
            </button>
          )}
        </section>
      ) : (
        <section className="flex flex-col gap-3">
          {overdueTasks.length > 0 ? (
            <>
              <p className="text-sm text-slate-400">
                Pendentes de dias anteriores desta semana.
              </p>
              <ul className="flex flex-col gap-2">
                {overdueTasks.map((task) => (
                  <li key={task.id}>
                    <TaskRow
                      task={task}
                      pending={pendingTaskId === task.id}
                      showDate
                      onToggle={handleToggle}
                    />
                  </li>
                ))}
              </ul>
            </>
          ) : (
            <div className="rounded-2xl bg-white/5 px-5 py-8 text-center ring-1 ring-white/10">
              {isFirstDayOfWeek ? (
                // Neutro, e sem o verde: não houve conquista a celebrar, a
                // semana apenas começou.
                <>
                  <CalendarDays
                    size={28}
                    className="mx-auto text-slate-400"
                    aria-hidden="true"
                  />
                  <p className="mt-2 font-medium text-slate-200">
                    Comece a semana zerado.
                  </p>
                  <p className="mt-1.5 text-sm text-slate-400">
                    Ainda não há dias anteriores nesta semana para cobrar.
                  </p>
                </>
              ) : (
                <>
                  <Check
                    size={28}
                    className="mx-auto text-brand-green"
                    aria-hidden="true"
                  />
                  <p className="mt-2 font-medium text-slate-200">
                    Nenhuma pendência da semana.
                  </p>
                  <p className="mt-1.5 text-sm text-slate-400">
                    Você não deixou nada para trás nos últimos dias.
                  </p>
                </>
              )}
            </div>
          )}
        </section>
      )}

      {/* Falha pontual: a lista continua na tela, só o aviso aparece. */}
      {error && (todayTasks.length > 0 || overdueTasks.length > 0) && (
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

export default Tarefas
