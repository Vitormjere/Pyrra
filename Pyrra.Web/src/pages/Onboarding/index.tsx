import { useState } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { Apple, Dumbbell, Flame, Sparkles, Wallet } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import Segmented from '../../components/Segmented'
import { useAuth } from '../../hooks/useAuth'
import { completeOnboarding } from '../../services/userService'
import { getApiErrorMessage } from '../../services/apiError'
import type { CommunicationTone } from '../../types/auth'

const TONES: readonly CommunicationTone[] = ['Direto', 'Acolhedor', 'Desafiador']

// Resumo visual dos módulos, mostrado na tela de boas-vindas: ícones (os mesmos
// da navegação) + nome curto + uma linha, para dar a visão geral do que o app faz
// sem virar um tutorial extenso.
const MODULES: readonly { name: string; description: string; icon: LucideIcon }[] = [
  { name: 'Foco & hábitos', description: 'Hábitos do dia com streak para manter a sequência', icon: Flame },
  { name: 'Treino', description: 'Registre academia e corrida', icon: Dumbbell },
  { name: 'Nutrição', description: 'Refeições do dia e plano da semana', icon: Apple },
  { name: 'Finanças', description: 'Anote entradas e saídas na mão', icon: Wallet },
  { name: 'Zelo', description: 'Assistente de IA que responde sobre seus dados', icon: Sparkles },
]

// Só o PRIMEIRO nome, nunca o e-mail. Campo vazio ou que claramente não é nome
// (contém "@", como uma conta que digitou o e-mail no lugar do nome) devolve null,
// e o cumprimento cai num "Bem-vindo!" neutro em vez de exibir o e-mail.
function firstNameOf(name: string): string | null {
  const first = name.trim().split(/\s+/)[0] ?? ''
  if (!first || first.includes('@')) {
    return null
  }
  return first
}

// Espelha os textos do Perfil, para o significado de cada tom ser o mesmo nos
// dois lugares onde ele é escolhido.
const TONE_HINTS: Record<CommunicationTone, string> = {
  Direto: 'Objetivo, sem rodeios.',
  Acolhedor: 'Gentil e compreensivo.',
  Desafiador: 'Provocador, te cutuca a ir além.',
}

// Horário sensato para a mensagem noturna quando o usuário nunca escolheu um: o
// registro nasce com 00:00 (meia-noite), que não serve como lembrete.
const DEFAULT_NOTIFICATION_TIME = '21:00'

const TOTAL_STEPS = 3

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

  // A sessão é garantida pelo ProtectedRoute; o guard é só defensivo.
  if (!user) return null

  // Acesso manual à URL depois de concluído não deve reabrir o fluxo.
  if (user.onboardingCompleted) return <Navigate to="/hoje" replace />

  // Chama o backend com as preferências (ou só o horário, ao pular), aplica o
  // usuário atualizado no contexto e cai na tela Hoje. O gate RequireOnboarding
  // deixa de redirecionar assim que onboardingCompleted vira true.
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
    // Em caso de sucesso não zeramos submitting: a tela é substituída por /hoje.
  }

  // Concluir: salva as duas preferências escolhidas.
  function handleFinish() {
    void finish({ communicationTone: tone, eveningNotificationTime: time })
  }

  // Pular: mantém o tom no default do registro e só aplica 21:00 como horário,
  // para a mensagem noturna não ficar à meia-noite sem o usuário ter escolhido.
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
                Como o Pyrra fala com você?
              </h1>
              <p className="mt-2 text-sm text-slate-400">
                Escolha o tom das mensagens. Dá para mudar depois no Perfil.
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
            </div>
          )}

          {step === 2 && (
            <div>
              <h1 className="glow-ink font-display text-2xl font-semibold tracking-tight text-ink">
                Que horas te lembramos?
              </h1>
              <p className="mt-2 text-sm text-slate-400">
                Toda noite o Pyrra manda um resumo do seu dia. Escolha o horário.
              </p>
              <div className="mt-6 flex flex-col gap-1.5">
                <label
                  htmlFor="onboarding-horario"
                  className="text-xs font-medium text-slate-400"
                >
                  Horário da mensagem noturna
                </label>
                <input
                  id="onboarding-horario"
                  type="time"
                  value={time}
                  onChange={(event) => setTime(event.target.value)}
                  className={inputClasses}
                />
              </div>
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
