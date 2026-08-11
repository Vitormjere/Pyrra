import { useEffect, useRef, useState } from 'react'
import type { FormEvent, KeyboardEvent } from 'react'
import { Check, Sparkles, X } from 'lucide-react'
import { confirmZeloPlanEdit, dismissZeloPlanEdit, getZeloPlanMessages, sendZeloPlanMessage } from '../services/zeloPlanService'
import { getApiErrorMessage } from '../services/apiError'
import { formatPlannedExercise } from '../utils/format'
import { WEEK_DAY_LABELS } from '../types/plan'
import type { ZeloEditProposalResponse, ZeloPlanChatMessageResponse } from '../types/zeloPlan'
import type { MealType } from '../types/nutrition'

// mesmo teto do backend
const MAX_LENGTH = 300

// mesmos rótulos do ZeloPlanPreviewStep
const MEAL_LABELS: Record<MealType, string> = {
  CafeDaManha: 'Café da manhã',
  Almoco: 'Almoço',
  Lanche: 'Lanche',
  Jantar: 'Jantar',
}

// resumo do "depois" da proposta — mesmo formato do preview do plano gerado
function EditProposalSummary({ proposal }: { proposal: ZeloEditProposalResponse }) {
  return (
    <div className="mt-2 rounded-md bg-brand-dark px-3 py-2 ring-1 ring-line">
      <p className="text-xs font-medium text-slate-400">
        {WEEK_DAY_LABELS[proposal.dayOfWeek]}
        {proposal.target === 'Nutricao' && proposal.mealType && ` · ${MEAL_LABELS[proposal.mealType]}`}
      </p>
      {proposal.target === 'Treino' ? (
        <>
          <p className="mt-0.5 text-sm text-ink">{proposal.label ?? 'Descanso'}</p>
          {proposal.exercises && proposal.exercises.length > 0 && (
            <ul className="mt-1 flex flex-col gap-0.5">
              {proposal.exercises.map((exercise, index) => (
                <li key={index} className="text-xs text-slate-400">
                  {exercise.type === 'Corrida'
                    ? exercise.exerciseName
                    : formatPlannedExercise(exercise.exerciseName, exercise.sets, exercise.reps)}
                </li>
              ))}
            </ul>
          )}
        </>
      ) : (
        <ul className="mt-1 flex flex-col gap-0.5">
          {(proposal.items ?? []).map((item, index) => (
            <li key={index} className="text-xs text-slate-400">
              {item.itemName} ({item.quantity})
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

interface ZeloPlanChatStepProps {
  sessionId: string
  onClose: () => void
}

// chat livre pós-formulário — mesmo estilo visual do ZeloCard, mas com histórico completo
// (multi-turno) em vez de uma pergunta isolada sem memória
export function ZeloPlanChatStep({ sessionId, onClose }: ZeloPlanChatStepProps) {
  const [messages, setMessages] = useState<ZeloPlanChatMessageResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [text, setText] = useState('')
  const [sending, setSending] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [resolvingId, setResolvingId] = useState<string | null>(null)
  const [resolveError, setResolveError] = useState<string | null>(null)
  const bottomRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const history = await getZeloPlanMessages(sessionId)
        if (active) setMessages(history)
      } catch {
        // silencioso: chat começa vazio, o usuário ainda consegue escrever
      } finally {
        if (active) setLoading(false)
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [sessionId])

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const trimmed = text.trim()
    if (!trimmed || sending) return

    setSending(true)
    setError(null)

    // otimista: mostra a pergunta na hora — o backend já a salva mesmo se a resposta falhar,
    // então não há risco de "perder" a mensagem do usuário na próxima carga do histórico
    const optimisticId = `pending-${Date.now()}`
    setMessages((current) => [
      ...current,
      {
        id: optimisticId,
        role: 'Usuario',
        content: trimmed,
        createdAt: new Date().toISOString(),
        editStatus: 'Nenhuma',
        editProposal: null,
      },
    ])
    setText('')

    try {
      const result = await sendZeloPlanMessage(sessionId, trimmed)
      if (result.reply) {
        setMessages((current) => [...current, result.reply!])
      } else {
        setError(result.error ?? 'O Zelo não conseguiu responder agora.')
      }
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível enviar sua mensagem.'))
    } finally {
      setSending(false)
    }
  }

  // enter envia, shift+enter quebra linha — mesmo atalho do ZeloCard
  function handleKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault()
      event.currentTarget.form?.requestSubmit()
    }
  }

  async function handleConfirmEdit(messageId: string) {
    setResolvingId(messageId)
    setResolveError(null)
    try {
      await confirmZeloPlanEdit(sessionId, messageId)
      setMessages((current) =>
        current.map((m) => (m.id === messageId ? { ...m, editStatus: 'Aplicada' } : m)),
      )
    } catch (err) {
      setResolveError(getApiErrorMessage(err, {}, 'Não foi possível aplicar essa edição.'))
    } finally {
      setResolvingId(null)
    }
  }

  async function handleDismissEdit(messageId: string) {
    setResolvingId(messageId)
    setResolveError(null)
    try {
      await dismissZeloPlanEdit(sessionId, messageId)
      setMessages((current) =>
        current.map((m) => (m.id === messageId ? { ...m, editStatus: 'Descartada' } : m)),
      )
    } catch (err) {
      setResolveError(getApiErrorMessage(err, {}, 'Não foi possível cancelar essa edição.'))
    } finally {
      setResolvingId(null)
    }
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-center gap-2">
        <Sparkles size={16} className="glow-icon shrink-0 text-brand-green" aria-hidden="true" />
        <p className="text-sm text-slate-400">Tire dúvidas ou peça um ajuste pontual no plano.</p>
      </div>

      <div className="flex max-h-64 flex-col gap-2 overflow-y-auto rounded-md bg-brand-dark p-3 ring-1 ring-line">
        {loading && <p className="text-center text-sm text-slate-500">Carregando conversa...</p>}
        {!loading && messages.length === 0 && (
          <p className="text-center text-sm text-slate-500">Pergunte o que quiser sobre o plano.</p>
        )}
        {messages.map((message) => (
          <div
            key={message.id}
            className={[
              'max-w-[85%] rounded-md px-3 py-2 text-sm leading-relaxed',
              message.role === 'Usuario'
                ? 'self-end bg-brand-green/10 text-ink'
                : 'self-start bg-surface-hi text-slate-200',
            ].join(' ')}
          >
            {message.content}

            {message.editStatus === 'Proposta' && message.editProposal && (
              <>
                <EditProposalSummary proposal={message.editProposal} />
                <div className="mt-2 flex gap-2">
                  <button
                    type="button"
                    disabled={resolvingId === message.id}
                    onClick={() => handleConfirmEdit(message.id)}
                    className="inline-flex flex-1 items-center justify-center gap-1 rounded-lg bg-brand-green px-2.5 py-1.5 text-xs font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    <Check size={13} aria-hidden="true" />
                    {resolvingId === message.id ? 'Aplicando...' : 'Aplicar'}
                  </button>
                  <button
                    type="button"
                    disabled={resolvingId === message.id}
                    onClick={() => handleDismissEdit(message.id)}
                    className="inline-flex items-center justify-center gap-1 rounded-lg px-2.5 py-1.5 text-xs font-medium text-slate-400 ring-1 ring-line transition hover:bg-surface-hi hover:text-ink disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    <X size={13} aria-hidden="true" />
                    Cancelar
                  </button>
                </div>
              </>
            )}
            {message.editStatus === 'Aplicada' && (
              <p className="mt-1.5 flex items-center gap-1 text-xs text-brand-green">
                <Check size={12} aria-hidden="true" />
                Aplicado
              </p>
            )}
            {message.editStatus === 'Descartada' && (
              <p className="mt-1.5 text-xs text-slate-500">Edição cancelada.</p>
            )}
          </div>
        ))}
        <div ref={bottomRef} />
      </div>

      {error && (
        <p role="alert" className="text-sm text-red-300">
          {error}
        </p>
      )}
      {resolveError && (
        <p role="alert" className="text-sm text-red-300">
          {resolveError}
        </p>
      )}

      <form onSubmit={handleSubmit} className="flex flex-col gap-2">
        <label htmlFor="zelo-plan-chat-input" className="sr-only">
          Sua mensagem
        </label>
        <textarea
          id="zelo-plan-chat-input"
          value={text}
          onChange={(event) => setText(event.target.value)}
          onKeyDown={handleKeyDown}
          rows={2}
          maxLength={MAX_LENGTH}
          disabled={sending}
          placeholder="Ex.: troca o treino de terça pra pernas"
          className="w-full resize-y rounded-md bg-brand-dark px-4 py-3 text-sm leading-relaxed text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green disabled:opacity-60"
        />
        <div className="flex gap-2">
          <button
            type="submit"
            disabled={sending || text.trim().length === 0}
            className="flex-1 rounded-xl bg-brand-green px-4 py-2.5 text-sm font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {sending ? 'Enviando...' : 'Enviar'}
          </button>
          <button
            type="button"
            onClick={onClose}
            className="rounded-xl px-4 py-2.5 text-sm font-medium text-slate-400 ring-1 ring-line transition hover:bg-surface-hi hover:text-ink"
          >
            Fechar
          </button>
        </div>
      </form>
    </div>
  )
}

export default ZeloPlanChatStep
