import { useCallback, useEffect, useState } from 'react'
import { NotebookPen } from 'lucide-react'
import SectionHeader from '../../components/SectionHeader'
import { getHistory } from '../../services/planningService'
import { getApiErrorMessage } from '../../services/apiError'
import { formatDayLabel } from '../../utils/format'
import type { PlanNoteResponse } from '../../types/planning'

const HISTORY_DAYS = 30

function LoadingState() {
  return (
    <div className="flex flex-col gap-3" aria-busy="true" aria-label="Carregando">
      <div className="h-24 animate-pulse rounded-md bg-surface" />
      <div className="h-24 animate-pulse rounded-md bg-surface" />
      <div className="h-24 animate-pulse rounded-md bg-surface" />
    </div>
  )
}

export function Diario() {
  const [notes, setNotes] = useState<PlanNoteResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const fetchHistory = useCallback(() => getHistory(HISTORY_DAYS), [])

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const data = await fetchHistory()
        if (active) setNotes(data)
      } catch (err) {
        if (active) {
          setError(
            getApiErrorMessage(err, {}, 'Não foi possível carregar seu diário.'),
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
  }, [fetchHistory])

  async function handleRetry() {
    setLoading(true)
    setError(null)
    try {
      setNotes(await fetchHistory())
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível carregar seu diário.'))
    } finally {
      setLoading(false)
    }
  }

  if (loading) return <LoadingState />

  if (error) {
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

  return (
    <div className="flex flex-col gap-5">
      <header>
        <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">Diário</h1>
        <p className="mt-1 text-sm text-slate-500">
          Suas reflexões dos últimos {HISTORY_DAYS} dias.
        </p>
      </header>

      {notes.length > 0 ? (
        <section className="flex flex-col gap-3">
          <SectionHeader>Reflexões</SectionHeader>

          {notes.map((note) => (
            <article
              key={note.date}
              className="rounded-md bg-surface px-5 py-4 ring-1 ring-line"
            >
              <h2 className="text-[11px] font-semibold tracking-[0.14em] text-slate-500 uppercase">
                {formatDayLabel(note.date)}
              </h2>
              {/* whitespace-pre-line preserva as quebras de linha que o usuário
                  digitou — sem isso o texto viraria um parágrafo único. */}
              <p className="mt-2 text-sm leading-relaxed whitespace-pre-line text-slate-200">
                {note.content}
              </p>
            </article>
          ))}
        </section>
      ) : (
        <div className="rounded-md bg-surface px-5 py-10 text-center ring-1 ring-line">
          <NotebookPen
            size={28}
            className="mx-auto text-slate-500"
            aria-hidden="true"
          />
          <p className="mt-3 font-medium text-slate-200">
            Nenhuma reflexão ainda.
          </p>
          <p className="mt-1.5 text-sm text-slate-400">
            Escreva no campo "Reflexão do dia" na tela Hoje e ela aparecerá
            aqui.
          </p>
        </div>
      )}
    </div>
  )
}

export default Diario
