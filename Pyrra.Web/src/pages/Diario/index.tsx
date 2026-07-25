import { useCallback, useEffect, useState } from 'react'
import { NotebookPen } from 'lucide-react'
import SectionHeader from '../../components/SectionHeader'
import Skeleton from '../../components/Skeleton'
import EmptyState from '../../components/EmptyState'
import ErrorRetry from '../../components/ErrorRetry'
import { getHistory } from '../../services/planningService'
import { getApiErrorMessage } from '../../services/apiError'
import { formatDayLabel } from '../../utils/format'
import type { PlanNoteResponse } from '../../types/planning'

const HISTORY_DAYS = 30

function LoadingState() {
  return (
    <div className="flex flex-col gap-3" aria-busy="true" aria-label="Carregando">
      <Skeleton className="h-24" />
      <Skeleton className="h-24" />
      <Skeleton className="h-24" />
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

  if (error) return <ErrorRetry message={error} onRetry={handleRetry} />

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
        <EmptyState
          icon={NotebookPen}
          title="Nenhuma reflexão ainda."
          description='Escreva no campo "Reflexão do dia" na tela Hoje e ela aparecerá aqui.'
        />
      )}
    </div>
  )
}

export default Diario
