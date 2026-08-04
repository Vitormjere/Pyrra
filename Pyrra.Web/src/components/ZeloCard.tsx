import { useState } from 'react'
import type { FormEvent, KeyboardEvent } from 'react'
import { Sparkles } from 'lucide-react'
import { askZelo } from '../services/zeloService'
import { getApiErrorMessage } from '../services/apiError'

// mesmo teto do backend (MaxQuestionLength) pra não deixar digitar o que a API vai recusar
const MAX_LENGTH = 300

// carrega o próprio estado, não depende da tela Hoje — é isolado, falha aqui não afeta o resto
export function ZeloCard() {
  const [question, setQuestion] = useState('')
  const [answer, setAnswer] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const pergunta = question.trim()
    if (!pergunta || loading) return

    setLoading(true)
    setError(null)

    try {
      const resposta = await askZelo(pergunta)
      setAnswer(resposta)
    } catch (err) {
      // 429 e validações já vêm com { message } pronto do backend, o resto cai no texto genérico
      setError(
        getApiErrorMessage(
          err,
          {},
          'O Zelo não conseguiu responder agora. Tente novamente em alguns instantes.',
        ),
      )
    } finally {
      setLoading(false)
    }
  }

  // enter envia, shift+enter quebra linha
  function handleKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault()
      event.currentTarget.form?.requestSubmit()
    }
  }

  return (
    <section className="flex flex-col gap-3 rounded-2xl bg-surface px-5 py-5 ring-1 ring-line">
      <div className="flex items-center gap-2">
        <Sparkles size={18} className="glow-icon shrink-0 text-brand-green" aria-hidden="true" />
        {/* Panchang no nome, como o resto dos títulos do app. */}
        <h2 className="glow-ink font-display text-xl font-semibold tracking-tight text-ink">
          Zelo
        </h2>
      </div>

      <form onSubmit={handleSubmit} className="flex flex-col gap-2">
        <label htmlFor="zelo-pergunta" className="sr-only">
          Sua pergunta ao Zelo
        </label>
        <textarea
          id="zelo-pergunta"
          value={question}
          onChange={(event) => {
            setQuestion(event.target.value)
            if (error !== null) setError(null)
          }}
          onKeyDown={handleKeyDown}
          rows={2}
          maxLength={MAX_LENGTH}
          disabled={loading}
          placeholder="Pergunte ao Zelo sobre seus hábitos, treinos ou alimentação..."
          className="w-full resize-y rounded-md bg-brand-dark px-4 py-3 text-sm leading-relaxed text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green disabled:opacity-60"
        />

        <button
          type="submit"
          disabled={loading || question.trim().length === 0}
          className="flex items-center justify-center gap-2 rounded-xl bg-brand-green px-4 py-2.5 font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {loading ? 'Pensando...' : 'Perguntar'}
        </button>
      </form>

      {error && (
        <p role="alert" className="text-sm text-red-300">
          {error}
        </p>
      )}

      {answer && !error && (
        // aria-live pra anunciar a resposta assim que chega, sem o usuário precisar procurar
        <div
          aria-live="polite"
          className="rounded-md bg-brand-dark px-4 py-3 text-sm leading-relaxed text-slate-200 ring-1 ring-line"
        >
          {answer}
        </div>
      )}
    </section>
  )
}

export default ZeloCard
