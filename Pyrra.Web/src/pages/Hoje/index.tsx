import { useCallback, useEffect, useState } from 'react'
import {
  Check,
  Dumbbell,
  Eye,
  EyeOff,
  Flame,
  ListChecks,
  Snowflake,
  Sparkles,
  Wallet,
} from 'lucide-react'
import MilestoneCelebration from '../../components/MilestoneCelebration'
import PreviewCard from '../../components/PreviewCard'
import { getDailyScore, toggleCheckIn } from '../../services/focusService'
import {
  acknowledgeMilestones,
  getPendingMilestones,
  getStreakStatus,
} from '../../services/streakService'
import { getBalance } from '../../services/financeService'
import { getWorkouts } from '../../services/workoutService'
import { getTasksForDay } from '../../services/taskService'
import { getApiErrorMessage } from '../../services/apiError'
import type { DailyScoreResponse } from '../../types/focus'
import type {
  PendingMilestoneResponse,
  StreakStatusResponse,
} from '../../types/streak'
import type { BalanceResponse } from '../../types/finance'
import type { WorkoutResponse } from '../../types/workout'
import type { TaskResponse } from '../../types/task'

// Formata "YYYY-MM-DD" sem passar por new Date(string): o parser trataria a
// string como UTC e, em fusos negativos como o Brasil, exibiria o dia anterior.
// Montar com os componentes numéricos mantém a data no calendário local.
function formatDayLabel(isoDate: string): string {
  const [year, month, day] = isoDate.split('-').map(Number)
  return new Intl.DateTimeFormat('pt-BR', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
  }).format(new Date(year, month - 1, day))
}

function toPercent(fraction: number): number {
  return Math.round(fraction * 100)
}

const currencyFormatter = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL',
})

// Sem casas fixas: 5 km sai "5 km" e não "5,00 km"; 7,5 km continua "7,5 km".
const numberFormatter = new Intl.NumberFormat('pt-BR', {
  maximumFractionDigits: 2,
})

// Uma prévia que falha não deve derrubar o dashboard inteiro. Converter a
// rejeição em null deixa o Promise.all resolver e cada card decide o que
// mostrar quando seu dado não veio.
function optional<T>(promise: Promise<T>): Promise<T | null> {
  return promise.catch(() => null)
}

// Resumo do treino conforme a modalidade: cada tipo tem campos próprios
// preenchidos, e os do outro tipo chegam nulos.
function describeWorkout(workout: WorkoutResponse): string {
  if (workout.type === 'Academia') {
    const parts = [workout.exerciseName ?? 'Treino']
    if (workout.loadKg !== null) {
      parts.push(`${numberFormatter.format(workout.loadKg)} kg`)
    }
    return parts.join(' · ')
  }

  const parts: string[] = []
  if (workout.distanceKm !== null) {
    parts.push(`${numberFormatter.format(workout.distanceKm)} km`)
  }
  if (workout.durationMinutes !== null) {
    parts.push(`${workout.durationMinutes} min`)
  }
  return parts.length > 0 ? parts.join(' · ') : 'Corrida registrada'
}

function LoadingState() {
  return (
    <div className="flex flex-col gap-4" aria-busy="true" aria-label="Carregando">
      <div className="h-28 animate-pulse rounded-2xl bg-white/5" />
      <div className="h-16 animate-pulse rounded-2xl bg-white/5" />
      <div className="h-14 animate-pulse rounded-xl bg-white/5" />
      <div className="h-14 animate-pulse rounded-xl bg-white/5" />
    </div>
  )
}

export function Hoje() {
  const [score, setScore] = useState<DailyScoreResponse | null>(null)
  const [streak, setStreak] = useState<StreakStatusResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  // Guarda QUAL foco está em voo, não um booleano: só a linha tocada trava,
  // as outras seguem clicáveis.
  const [pendingFocusId, setPendingFocusId] = useState<string | null>(null)
  // Fila de celebrações: exibimos uma por vez e vamos consumindo pela frente.
  const [milestones, setMilestones] = useState<PendingMilestoneResponse[]>([])
  const [acknowledging, setAcknowledging] = useState(false)

  // Prévias dos módulos. null = a chamada falhou; o card mostra indisponível em
  // vez de sumir, para a ausência não parecer "sem dados".
  const [balance, setBalance] = useState<BalanceResponse | null>(null)
  const [workouts, setWorkouts] = useState<WorkoutResponse[] | null>(null)
  const [tasks, setTasks] = useState<TaskResponse[] | null>(null)
  // Saldo começa oculto: o dashboard abre logo ao entrar no app, muitas vezes em
  // público. Estado só de sessão, não persistido.
  const [balanceVisible, setBalanceVisible] = useState(false)

  // Só busca, não mexe em estado: assim o efeito abaixo consegue adiar todo
  // setState para depois do await, como a regra react-hooks/set-state-in-effect
  // exige, e a mesma função serve ao "tentar de novo".
  const fetchDay = useCallback(async () => {
    // Em paralelo: são endpoints independentes, e em série a tela levaria a
    // soma dos dois tempos para aparecer.
    // score e streak são o núcleo: sem eles a tela não tem o que mostrar, então
    // a falha deles derruba o carregamento. As três prévias passam por
    // optional() — cada uma degrada sozinha sem levar o dashboard junto.
    const [scoreData, streakData, balanceData, workoutsData, tasksData] =
      await Promise.all([
        getDailyScore(),
        getStreakStatus(),
        optional(getBalance()),
        optional(getWorkouts()),
        optional(getTasksForDay()),
      ])

    // Depois, nunca junto: é o acerto rodado dentro do GET /api/streak que grava
    // os marcos. Em paralelo, um marco criado agora poderia não estar na lista.
    const pending = await getPendingMilestones()

    return { scoreData, streakData, balanceData, workoutsData, tasksData, pending }
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
        setWorkouts(result.workoutsData)
        setTasks(result.tasksData)
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
      setWorkouts(result.workoutsData)
      setTasks(result.tasksData)
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
  const latestWorkout = workouts?.[0]
  const todayWorkout =
    latestWorkout && latestWorkout.date === score.date ? latestWorkout : null

  const pendingTasks = tasks?.filter((task) => !task.completed).length ?? 0

  return (
    <div className="flex flex-col gap-5">
      <header>
        <h1 className="font-display text-3xl tracking-tight">Hoje</h1>
        <p className="mt-1 text-sm text-slate-400 first-letter:uppercase">
          {formatDayLabel(score.date)}
        </p>
      </header>

      {/*
        CARD DO FOGUINHO
        Superfície elevada no estado normal; preenchimento sólido em brand-green
        só quando a meta do dia é batida. Verde sólido é o sinal mais forte da
        tela e fica reservado para ação (botão) e conquista — se fosse o fundo
        padrão do card, perderia o significado e brigaria com o botão primário.
      */}
      <section
        className={[
          'flex items-center justify-between rounded-2xl px-5 py-4 transition',
          streak.todayGoalMet
            ? 'bg-brand-green text-brand-dark'
            : 'bg-white/5 text-slate-100 ring-1 ring-white/10',
        ].join(' ')}
      >
        <div className="flex items-center gap-3">
          <Flame
            size={36}
            strokeWidth={2}
            aria-hidden="true"
            className={streak.todayGoalMet ? 'text-brand-dark' : 'text-brand-green'}
          />
          <div>
            <p className="text-4xl leading-none font-semibold tabular-nums">
              {streak.displayCount}
            </p>
            <p
              className={[
                'mt-1 text-xs',
                streak.todayGoalMet ? 'text-brand-dark/70' : 'text-slate-400',
              ].join(' ')}
            >
              {streak.displayCount === 1 ? 'dia seguido' : 'dias seguidos'}
            </p>
          </div>
        </div>

        {/* Freezes: informação de apoio, então fica discreta ao lado. */}
        <div
          className={[
            'flex items-center gap-1.5 rounded-full px-3 py-1.5 text-sm',
            streak.todayGoalMet ? 'bg-brand-dark/10' : 'bg-white/5',
          ].join(' ')}
          title={`${streak.freezesAvailable} freeze(s) disponível(is)`}
        >
          <Snowflake size={16} aria-hidden="true" />
          <span className="font-medium tabular-nums">
            {streak.freezesAvailable}
          </span>
          <span className="sr-only">freezes disponíveis</span>
        </div>
      </section>

      {/* PROGRESSO DO DIA */}
      <section className="rounded-2xl bg-white/5 px-5 py-4 ring-1 ring-white/10">
        <div className="flex items-baseline justify-between">
          <h2 className="text-sm font-medium text-slate-300">Meta do dia</h2>
          <span className="text-sm font-semibold tabular-nums">{percent}%</span>
        </div>

        <div
          className="mt-3 h-2.5 w-full overflow-hidden rounded-full bg-white/10"
          role="progressbar"
          aria-valuenow={percent}
          aria-valuemin={0}
          aria-valuemax={100}
          aria-label="Progresso da meta do dia"
        >
          {/* Meta batida muda a cor de verde apagado para o verde pleno. */}
          <div
            className={[
              'h-full rounded-full transition-all duration-300',
              score.goalMet ? 'bg-brand-green' : 'bg-brand-green/50',
            ].join(' ')}
            style={{ width: `${percent}%` }}
          />
        </div>

        <p className="mt-2 flex items-center gap-1.5 text-xs text-slate-400">
          {score.goalMet ? (
            <>
              <Sparkles size={14} className="text-brand-green" aria-hidden="true" />
              <span className="font-medium text-brand-green">
                Meta batida hoje
              </span>
            </>
          ) : (
            <span className="tabular-nums">
              {score.pointsEarned} de {score.pointsPossible} pontos
            </span>
          )}
        </p>
      </section>

      {/* LISTA DE FOCOS */}
      <section className="flex flex-col gap-2">
        <h2 className="text-sm font-medium text-slate-300">Seus focos</h2>

        {hasFocuses ? (
          <ul className="flex flex-col gap-2">
            {score.focuses.map((focus) => {
              const pending = pendingFocusId === focus.focusId
              return (
                <li key={focus.focusId}>
                  {/*
                    A linha inteira é o botão: em 390px, um alvo de toque do
                    tamanho da linha erra muito menos que um quadradinho de 22px.
                  */}
                  <button
                    type="button"
                    role="checkbox"
                    aria-checked={focus.completed}
                    disabled={pending}
                    onClick={() => handleToggle(focus.focusId)}
                    className="flex w-full items-center gap-3 rounded-xl bg-white/5 px-4 py-3.5 text-left ring-1 ring-white/10 transition hover:bg-white/10 disabled:opacity-50"
                  >
                    <span
                      aria-hidden="true"
                      className={[
                        'flex size-6 shrink-0 items-center justify-center rounded-md border-2 transition',
                        focus.completed
                          ? 'border-brand-green bg-brand-green text-brand-dark'
                          : 'border-white/25',
                      ].join(' ')}
                    >
                      {focus.completed && <Check size={16} strokeWidth={3} />}
                    </span>

                    <span
                      className={[
                        'flex-1 transition',
                        focus.completed
                          ? 'text-slate-400 line-through'
                          : 'text-slate-100',
                      ].join(' ')}
                    >
                      {focus.name}
                    </span>

                    <span className="shrink-0 text-xs text-slate-500 tabular-nums">
                      peso {focus.weight}
                    </span>
                  </button>
                </li>
              )
            })}
          </ul>
        ) : (
          <div className="rounded-2xl bg-white/5 px-5 py-8 text-center ring-1 ring-white/10">
            <p className="font-medium text-slate-200">
              Você ainda não tem focos.
            </p>
            <p className="mt-1.5 text-sm text-slate-400">
              Focos são os hábitos que você quer seguir todo dia. Crie o
              primeiro para o seu foguinho começar a contar.
            </p>
          </div>
        )}
      </section>

      {/* PRÉVIAS DOS MÓDULOS — cada card leva à tela completa. */}
      <section className="flex flex-col gap-3">
        <h2 className="text-sm font-medium text-slate-300">Resumo</h2>

        <PreviewCard
          to="/financas"
          title="Finanças"
          icon={Wallet}
          action={
            <button
              type="button"
              onClick={() => setBalanceVisible((visible) => !visible)}
              aria-pressed={balanceVisible}
              aria-label={balanceVisible ? 'Ocultar saldo' : 'Mostrar saldo'}
              className="rounded-lg p-1.5 text-slate-400 transition hover:bg-white/10 hover:text-slate-200"
            >
              {balanceVisible ? <Eye size={16} /> : <EyeOff size={16} />}
            </button>
          }
        >
          {balance ? (
            <p className="text-2xl font-semibold tabular-nums">
              {balanceVisible
                ? currencyFormatter.format(balance.currentBalance)
                : 'R$ ••••••'}
            </p>
          ) : (
            <p className="text-sm text-slate-500">Saldo indisponível agora.</p>
          )}
        </PreviewCard>

        <PreviewCard to="/treino" title="Treino" icon={Dumbbell}>
          {workouts === null ? (
            <p className="text-sm text-slate-500">Treinos indisponíveis agora.</p>
          ) : todayWorkout ? (
            <div>
              <p className="font-medium">{describeWorkout(todayWorkout)}</p>
              <p className="mt-0.5 text-xs text-slate-400">
                {todayWorkout.type}
              </p>
            </div>
          ) : (
            <p className="text-sm text-slate-400">Nenhum treino hoje.</p>
          )}
        </PreviewCard>

        <PreviewCard to="/tarefas" title="Tarefas" icon={ListChecks}>
          {tasks === null ? (
            <p className="text-sm text-slate-500">Tarefas indisponíveis agora.</p>
          ) : pendingTasks > 0 ? (
            <p className="font-medium">
              {pendingTasks}{' '}
              {pendingTasks === 1
                ? 'tarefa pendente hoje'
                : 'tarefas pendentes hoje'}
            </p>
          ) : tasks.length > 0 ? (
            // Havia tarefas e todas foram concluídas: isso é conquista, não vazio.
            <p className="font-medium text-brand-green">Tudo em dia!</p>
          ) : (
            <p className="text-sm text-slate-400">Nenhuma tarefa para hoje.</p>
          )}
        </PreviewCard>
      </section>

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
