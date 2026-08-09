import { useState } from 'react'
import type { FormEvent } from 'react'
import type { ZeloPlanQuestionResponse } from '../types/zeloPlan'

interface ZeloPlanQuestionStepProps {
  question: ZeloPlanQuestionResponse
  submitting: boolean
  onSubmit: (answer: string) => void
}

// pergunta de escolha única (botões) ou texto livre — decide sozinho pelo campo options
export function ZeloPlanQuestionStep({ question, submitting, onSubmit }: ZeloPlanQuestionStepProps) {
  const [freeText, setFreeText] = useState('')

  function handleFreeTextSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const trimmed = freeText.trim()
    if (!trimmed) return
    onSubmit(trimmed)
  }

  return (
    <div className="flex flex-col gap-4">
      <p className="text-base font-medium text-ink">{question.text}</p>

      {question.options ? (
        <div className="flex flex-col gap-2">
          {question.options.map((option) => (
            <button
              key={option}
              type="button"
              disabled={submitting}
              onClick={() => onSubmit(option)}
              className="rounded-xl bg-surface-hi px-4 py-3 text-left text-sm font-medium text-ink ring-1 ring-line transition hover:ring-brand-green disabled:cursor-not-allowed disabled:opacity-60"
            >
              {option}
            </button>
          ))}
        </div>
      ) : (
        <form onSubmit={handleFreeTextSubmit} className="flex flex-col gap-2">
          <label htmlFor="zelo-plan-free-text" className="sr-only">
            Sua resposta
          </label>
          <textarea
            id="zelo-plan-free-text"
            value={freeText}
            onChange={(event) => setFreeText(event.target.value)}
            rows={2}
            maxLength={300}
            disabled={submitting}
            placeholder="Digite sua resposta..."
            className="w-full resize-y rounded-md bg-brand-dark px-4 py-3 text-sm leading-relaxed text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green disabled:opacity-60"
          />
          <button
            type="submit"
            disabled={submitting || freeText.trim().length === 0}
            className="self-start rounded-xl bg-brand-green px-4 py-2.5 text-sm font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {submitting ? 'Enviando...' : 'Continuar'}
          </button>
        </form>
      )}
    </div>
  )
}

export default ZeloPlanQuestionStep
