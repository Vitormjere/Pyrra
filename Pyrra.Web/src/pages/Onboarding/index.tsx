import { useState } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { Apple, Dumbbell, Flame, Shield, Sparkles, Trophy, Users, Wallet } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import Segmented from '../../components/Segmented'
import { useAuth } from '../../hooks/useAuth'
import { completeOnboarding } from '../../services/userService'
import { getApiErrorMessage } from '../../services/apiError'
import type { CommunicationTone } from '../../types/auth'

const TONES: readonly CommunicationTone[] = ['Direto', 'Acolhedor', 'Desafiador']

// resumo visual dos módulos na tela de boas-vindas, pra dar a visão geral do app sem virar um tutorial
const MODULES: readonly { name: string; description: string; icon: LucideIcon }[] = [
  { name: 'Foco & hábitos', description: 'Hábitos do dia com streak para manter a sequência', icon: Flame },
  { name: 'Treino', description: 'Registre academia e corrida', icon: Dumbbell },
  { name: 'Nutrição', description: 'Refeições do dia e plano da semana', icon: Apple },
  { name: 'Finanças', description: 'Anote entradas e saídas na mão', icon: Wallet },
  { name: 'Zelo', description: 'Assistente de IA que responde sobre seus dados', icon: Sparkles },
]

// mesmo formato do MODULES acima, reaproveitado no passo de Comunidade — ícones batendo com os do menu (AppLayout)
const COMMUNITY_HIGHLIGHTS: readonly { name: string; description: string; icon: LucideIcon }[] = [
  { name: 'Amigos', description: 'Veja o streak de quem você conhece e mande uma força quando bater a preguiça.', icon: Users },
  { name: 'Times', description: 'Junte um grupo, ganhe pontos coletivos e acompanhe o desempenho de todo mundo.', icon: Shield },
  { name: 'Torneios', description: 'Entre em competições com desafios e dispute o topo do ranking.', icon: Trophy },
]

// só o primeiro nome, nunca o e-mail — vazio ou contendo "@" devolve null e o cumprimento cai num "Bem-vindo!" neutro
function firstNameOf(name: string): string | null {
  const first = name.trim().split(/\s+/)[0] ?? ''
  if (!first || first.includes('@')) {
    return null
  }
  return first
}

// espelha os textos de Configurações, pro significado de cada tom ser o mesmo nos dois lugares
const TONE_HINTS: Record<CommunicationTone, string> = {
  Direto: 'Direto ao ponto, sem rodeios.',
  Acolhedor: 'Gentil, no seu ritmo.',
  Desafiador: 'Provoca pra te tirar da inércia.',
}

// exemplo do resumo noturno em cada tom — mostra a diferença na prática em vez de só descrever
const TONE_PREVIEWS: Record<CommunicationTone, string> = {
  Direto: 'Você fechou 4 de 5 hábitos hoje. Falta o treino.',
  Acolhedor: 'Foi um baita dia — você deu conta de quase tudo. Só falta o treino, sem pressa.',
  Desafiador: '4 de 5. Vai deixar o treino furar sua sequência? Ainda dá tempo.',
}

// horário padrão pra mensagem noturna — o registro nasce com 00:00, que não serve como lembrete
const DEFAULT_NOTIFICATION_TIME = '21:00'

const TOTAL_STEPS = 4

const inputClasses =
  'w-full rounded-md bg-surface px-4 py-3 text-ink ring-1 ring-line transition outline-none focus:ring-2 focus:ring-brand-green'

const primaryButtonClasses =
  'flex-1 rounded-xl bg-brand-green px-4 py-3 font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60'

const secondaryButtonClasses =
  'rounded-xl px-4 py-3 font-medium text-slate-300 ring-1 ring-line transition hover:bg-surface-hi hover:text-ink disabled:opacity-60'

export function Onboarding() {
  const { user, applyUser } = useAuth()
  const navigate = useNavigate()

  const [step, setStep] = useState(0)
  const [tone, setTone] = useState<CommunicationTone>(
    user?.communicationTone ?? 'Direto',
  )
  const [time, setTime] = useState(
    user && user.eveningNotificationTime !== '00:00'
      ? user.eveningNotificationTime
      : DEFAULT_NOTIFICATION_TIME,
  )
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // a sessão já é garantida pelo ProtectedRoute, esse guard aqui é só defensivo
  if (!user) return null

  // acesso manual à URL depois de concluído não deve reabrir o fluxo
  if (user.onboardingCompleted) return <Navigate to="/hoje" replace />

  // chama o backend, aplica o usuário atualizado no contexto e cai na tela Hoje — o RequireOnboarding para de redirecionar assim que onboardingCompleted vira true
  async function finish(prefs?: {
    communicationTone?: CommunicationTone
    eveningNotificationTime?: string
  }) {
    setSubmitting(true)
    setError(null)

    try {
      const updated = await completeOnboarding(prefs)
      applyUser(updated)
      navigate('/hoje', { replace: true })
    } catch (err) {
      setError(
        getApiErrorMessage(
          err,
          {},
          'Não foi possível salvar. Tente novamente.',
        ),
      )
      setSubmitting(false)
    }
    // em caso de sucesso não zeramos submitting — a tela é substituída por /hoje
  }

  // concluir: salva as duas preferências escolhidas
  function handleFinish() {
    void finish({ communicationTone: tone, eveningNotificationTime: time })
  }

  // pular: mantém o tom padrão e só aplica 21:00 como horário, pra mensagem noturna não ficar à meia-noite
  function handleSkip() {
    void finish({ eveningNotificationTime: DEFAULT_NOTIFICATION_TIME })
  }

  const firstName = firstNameOf(user.name)

  return (
    <main className="flex min-h-screen flex-col px-4 py-8">
      <div className="mx-auto flex w-full max-w-sm flex-1 flex-col">
        {/* Cabeçalho: progresso à esquerda, "Fazer depois" sempre visível à direita. */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-1.5" aria-hidden="true">
            {Array.from({ length: TOTAL_STEPS }).map((_, index) => (
              <span
                key={index}
                className={[
                  'h-1.5 rounded-full transition-all',
                  index === step ? 'w-6 bg-brand-green' : 'w-1.5 bg-line',
                ].join(' ')}
              />
            ))}
          </div>
          <button
            type="button"
            onClick={handleSkip}
            disabled={submitting}
            className="text-xs font-medium text-slate-500 transition hover:text-slate-300 disabled:opacity-60"
          >
            Fazer depois
          </button>
        </div>

        {/* Conteúdo do passo, centralizado no espaço restante. */}
        <div className="flex flex-1 flex-col justify-center py-8">
          {step === 0 && (
            <div>
              <div className="text-center">
                <h1 className="glow-ink font-display text-2xl font-semibold tracking-tight text-ink">
                  {firstName ? `Bem-vindo, ${firstName}` : 'Bem-vindo!'}
                </h1>
                <p className="mt-2 text-sm text-slate-400">
                  Tudo para manter sua rotina em dia, num lugar só:
                </p>
              </div>

              {/* Visão geral dos módulos — ícone + nome + uma linha cada. */}
              <ul className="mt-6 flex flex-col gap-2">
                {MODULES.map(({ name, description, icon: Icon }) => (
                  <li
                    key={name}
                    className="flex items-center gap-3 rounded-md bg-surface px-3 py-2.5 ring-1 ring-line"
                  >
                    <span
                      aria-hidden="true"
                      className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-surface-hi"
                    >
                      <Icon size={18} className="text-slate-200" />
                    </span>
                    <div className="min-w-0">
                      <p className="text-sm font-medium text-ink">{name}</p>
                      <p className="text-xs text-slate-500">{description}</p>
                    </div>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {step === 1 && (
            <div>
              <h1 className="glow-ink font-display text-2xl font-semibold tracking-tight text-ink">
                Como você quer que a gente fale com você?
              </h1>
              <p className="mt-2 text-sm text-slate-400">
                Isso muda o tom das notificações e das respostas do Zelo. Dá pra trocar quando
                quiser, em Configurações.
              </p>
              <div className="mt-6 flex flex-col gap-2">
                <Segmented
                  label="Tom de comunicação"
                  options={TONES}
                  value={tone}
                  onChange={setTone}
                />
                <p className="text-sm text-slate-500">{TONE_HINTS[tone]}</p>
              </div>

              {/* preview do resumo noturno no tom escolhido — mostra a diferença em vez de só descrever */}
              <div className="mt-5 flex items-start gap-2.5">
                <span
                  aria-hidden="true"
                  className="flex size-8 shrink-0 items-center justify-center rounded-full bg-surface-hi ring-1 ring-line"
                >
                  <Sparkles size={15} className="text-brand-green" />
                </span>
                <div className="min-w-0 flex-1">
                  <p className="mb-1 text-xs text-slate-500">Assim soa o resumo noturno</p>
                  <div className="rounded-2xl rounded-bl-sm bg-surface-hi px-3.5 py-2.5 text-sm text-ink ring-1 ring-line">
                    {TONE_PREVIEWS[tone]}
                  </div>
                </div>
              </div>
            </div>
          )}

          {step === 2 && (
            <div>
              <h1 className="glow-ink font-display text-2xl font-semibold tracking-tight text-ink">
                Quando você quer seu resumo do dia?
              </h1>
              <p className="mt-2 text-sm text-slate-400">
                Todo fim de dia o Pyrra manda um resumo do que você fez — hábitos, treino, o que
                ficou pra trás. Escolha o horário que funciona pra você.
              </p>
              <div className="mt-6 flex flex-col gap-1.5">
                <label
                  htmlFor="onboarding-horario"
                  className="text-xs font-medium text-slate-400"
                >
                  Horário do resumo noturno
                </label>
                <input
                  id="onboarding-horario"
                  type="time"
                  value={time}
                  onChange={(event) => setTime(event.target.value)}
                  className={inputClasses}
                />
                <p className="text-sm text-slate-500">
                  Você recebe esse resumo hoje às {time}.
                </p>
              </div>
            </div>
          )}

          {step === 3 && (
            <div>
              <h1 className="glow-ink font-display text-2xl font-semibold tracking-tight text-ink">
                Você não precisa fazer isso sozinho
              </h1>
              <p className="mt-2 text-sm text-slate-400">
                Adicione amigos, monte um time, dispute torneios com desafios — isso também é
                Pyrra.
              </p>

              <ul className="mt-6 flex flex-col gap-2">
                {COMMUNITY_HIGHLIGHTS.map(({ name, description, icon: Icon }) => (
                  <li
                    key={name}
                    className="flex items-center gap-3 rounded-md bg-surface px-3 py-2.5 ring-1 ring-line"
                  >
                    <span
                      aria-hidden="true"
                      className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-surface-hi"
                    >
                      <Icon size={18} className="text-slate-200" />
                    </span>
                    <div className="min-w-0">
                      <p className="text-sm font-medium text-ink">{name}</p>
                      <p className="text-xs text-slate-500">{description}</p>
                    </div>
                  </li>
                ))}
              </ul>

              <p className="mt-4 text-center text-xs text-slate-500">
                Você pode explorar tudo isso depois, direto no menu.
              </p>
            </div>
          )}
        </div>

        {/* Navegação do passo. */}
        <div className="flex flex-col gap-3">
          <div className="flex gap-2">
            {step > 0 && (
              <button
                type="button"
                onClick={() => setStep((current) => current - 1)}
                disabled={submitting}
                className={secondaryButtonClasses}
              >
                Voltar
              </button>
            )}

            {step < TOTAL_STEPS - 1 ? (
              <button
                type="button"
                onClick={() => setStep((current) => current + 1)}
                className={primaryButtonClasses}
              >
                {step === 0 ? 'Começar' : 'Continuar'}
              </button>
            ) : (
              <button
                type="button"
                onClick={handleFinish}
                disabled={submitting}
                className={primaryButtonClasses}
              >
                {submitting ? 'Salvando...' : 'Concluir'}
              </button>
            )}
          </div>

          {error && (
            <p
              role="alert"
              className="rounded-lg bg-red-500/10 px-3 py-2 text-center text-sm text-red-300 ring-1 ring-red-500/20"
            >
              {error}
            </p>
          )}
        </div>
      </div>
    </main>
  )
}

export default Onboarding
