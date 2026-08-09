import { useEffect, useState } from 'react'
import { X } from 'lucide-react'
import ZeloPlanQuestionStep from './ZeloPlanQuestionStep'
import ZeloPlanPreviewStep from './ZeloPlanPreviewStep'
import { useConfirm } from '../hooks/useConfirm'
import { getApiErrorMessage } from '../services/apiError'
import {
  answerZeloPlanQuestion,
  applyZeloPlan,
  discardZeloPlan,
  getZeloPlanPreview,
  retryZeloPlanGeneration,
  startZeloPlan,
} from '../services/zeloPlanService'
import type { GeneratedPlanResponse, ZeloPlanSessionResponse } from '../types/zeloPlan'

interface ZeloPlanModalProps {
  onClose: () => void
  // chamado depois de aplicar com sucesso — o pai rebusca seu próprio plano (Treino ou Nutrição)
  onApplied: () => void
}

type Phase = 'loading' | 'question' | 'generating' | 'error' | 'preview' | 'applying'

// Modal único que cobre o fluxo inteiro: formulário guiado -> geração -> preview -> aplicar/descartar
// -> chat livre. Mesmo padrão de overlay do WorkoutTemplatePicker (fundo escuro, Esc fecha).
// A mesma sessão serve tanto o botão de Treino quanto o de Nutrição — iniciar sempre retoma a
// sessão ativa do usuário, então abrir o modal de qualquer aba continua de onde parou.
export function ZeloPlanModal({ onClose, onApplied }: ZeloPlanModalProps) {
  const [phase, setPhase] = useState<Phase>('loading')
  const [session, setSession] = useState<ZeloPlanSessionResponse | null>(null)
  const [plan, setPlan] = useState<GeneratedPlanResponse | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const { confirm, dialog } = useConfirm()

  // Esc fecha, como no WorkoutTemplatePicker — não durante uma ação em voo
  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape' && !submitting && phase !== 'applying') onClose()
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [onClose, submitting, phase])

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const state = await startZeloPlan()
        if (active) await enterState(state)
      } catch (err) {
        if (active) {
          setError(getApiErrorMessage(err, {}, 'Não foi possível iniciar o Zelo.'))
          setPhase('error')
        }
      }
    }

    void run()
    return () => {
      active = false
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  // decide a fase a partir do estado que o backend devolveu — usado no início e após cada resposta/retry
  async function enterState(state: ZeloPlanSessionResponse) {
    setSession(state)

    if (state.status === 'Coletando' && state.nextQuestion) {
      setPhase('question')
      return
    }

    if (state.status === 'Coletando' && !state.nextQuestion) {
      // formulário completo mas a geração falhou — error vem preenchido nesse caso
      setError(state.error ?? 'Não foi possível gerar o plano.')
      setPhase('error')
      return
    }

    if (state.status === 'PlanoGerado') {
      const preview = await getZeloPlanPreview(state.sessionId)
      setPlan(preview.plan)
      setPhase('preview')
      return
    }

    // Aplicada/Descartada não deveriam vir de iniciar/responder — por segurança, fecha
    onClose()
  }

  async function handleAnswer(answer: string) {
    if (!session) return
    setSubmitting(true)
    setError(null)

    try {
      const state = await answerZeloPlanQuestion(session.sessionId, answer)
      if (state.nextQuestion) {
        setSession(state)
      } else {
        setPhase('generating')
        await enterState(state)
      }
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível enviar sua resposta.'))
    } finally {
      setSubmitting(false)
    }
  }

  async function handleRetry() {
    setSubmitting(true)
    setError(null)

    try {
      const state = session
        ? await retryZeloPlanGeneration(session.sessionId)
        : await startZeloPlan()
      await enterState(state)
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível continuar.'))
    } finally {
      setSubmitting(false)
    }
  }

  async function handleApply() {
    if (!session) return
    setPhase('applying')
    setError(null)

    try {
      await applyZeloPlan(session.sessionId)
      onApplied()
      onClose()
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível aplicar o plano.'))
      setPhase('preview')
    }
  }

  async function handleDiscard() {
    if (!session) return

    const ok = await confirm({
      title: 'Descartar plano?',
      message: 'O plano gerado é perdido. Seu Treino e Nutrição continuam como estão.',
      confirmLabel: 'Descartar',
      destructive: true,
    })
    if (!ok) return

    try {
      await discardZeloPlan(session.sessionId)
    } catch {
      // sem ação útil a tomar — a sessão expira em 24h de qualquer forma
    } finally {
      onClose()
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <button
        type="button"
        aria-label="Fechar"
        onClick={() => !submitting && phase !== 'applying' && onClose()}
        className="absolute inset-0 bg-brand-dark/80 backdrop-blur-sm"
      />

      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="zelo-plan-title"
        className="relative flex max-h-[85vh] w-full max-w-lg flex-col rounded-md bg-surface ring-1 ring-line"
      >
        <div className="flex items-center justify-between border-b border-line px-5 py-4">
          <div>
            <h2 id="zelo-plan-title" className="font-display text-xl font-semibold tracking-tight text-ink">
              Zelo
            </h2>
            <p className="mt-0.5 text-sm text-slate-400">Monta seu Treino e Nutrição da semana.</p>
          </div>
          <button
            type="button"
            onClick={onClose}
            disabled={submitting || phase === 'applying'}
            aria-label="Fechar"
            className="shrink-0 rounded p-1 text-slate-500 transition hover:bg-surface-hi hover:text-ink disabled:opacity-50"
          >
            <X size={20} />
          </button>
        </div>

        <div className="flex-1 overflow-y-auto px-5 py-4">
          {phase === 'loading' && <p className="py-8 text-center text-sm text-slate-500">Carregando...</p>}

          {phase === 'question' && session?.nextQuestion && (
            <ZeloPlanQuestionStep question={session.nextQuestion} submitting={submitting} onSubmit={handleAnswer} />
          )}

          {phase === 'generating' && (
            <p className="py-8 text-center text-sm text-slate-500">O Zelo está montando seu plano...</p>
          )}

          {phase === 'error' && (
            <div className="flex flex-col items-center gap-3 py-8 text-center">
              <p className="text-sm text-red-300">{error}</p>
              <button
                type="button"
                onClick={handleRetry}
                disabled={submitting}
                className="rounded-xl bg-brand-green px-4 py-2 text-sm font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {submitting ? 'Tentando...' : 'Tentar de novo'}
              </button>
            </div>
          )}

          {phase === 'preview' && plan && (
            <ZeloPlanPreviewStep plan={plan} applying={false} error={error} onApply={handleApply} onDiscard={handleDiscard} />
          )}

          {phase === 'applying' && (
            <p className="py-8 text-center text-sm text-slate-500">Aplicando o plano...</p>
          )}
        </div>
      </div>

      {dialog}
    </div>
  )
}

export default ZeloPlanModal
