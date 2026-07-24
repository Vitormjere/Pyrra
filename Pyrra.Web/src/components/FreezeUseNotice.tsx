import { useEffect } from 'react'
import { Snowflake } from 'lucide-react'
import { formatShortDate } from '../utils/format'
import type { PendingFreezeUseResponse } from '../types/streak'

interface FreezeUseNoticeProps {
  freezeUse: PendingFreezeUseResponse
  /** Quantos avisos ainda faltam depois deste, para mostrar o progresso da fila. */
  remaining: number
  submitting: boolean
  onConfirm: () => void
}

// Mesmo formato de modal do MilestoneCelebration, com paleta fria (sky) em vez do
// verde de conquista: um freeze usado não é vitória a celebrar, é um alívio a
// comunicar — "sua sequência estava em risco e foi salva".
export function FreezeUseNotice({
  freezeUse,
  remaining,
  submitting,
  onConfirm,
}: FreezeUseNoticeProps) {
  // Esc fecha, como em qualquer diálogo — igual ao MilestoneCelebration.
  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        onConfirm()
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [onConfirm])

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-brand-dark/80 p-4 backdrop-blur-sm"
      role="dialog"
      aria-modal="true"
      aria-labelledby="freeze-title"
    >
      <div className="w-full max-w-sm rounded-md bg-surface px-6 py-8 text-center ring-1 ring-sky-400/40">
        <span
          aria-hidden="true"
          className="mx-auto flex size-16 items-center justify-center rounded-full ring-1 ring-sky-400/30"
        >
          <Snowflake size={30} strokeWidth={1.75} className="text-sky-400" />
        </span>

        <h2
          id="freeze-title"
          className="mt-4 font-display text-2xl font-semibold tracking-tight text-sky-400"
        >
          ❄️ Seu freeze te salvou!
        </h2>

        <p className="mt-2 text-sm text-slate-400">
          Você não perdeu a sequência em{' '}
          <span className="font-semibold text-slate-200 tabular-nums">
            {formatShortDate(freezeUse.date)}
          </span>
          .
        </p>

        <button
          type="button"
          autoFocus
          disabled={submitting}
          onClick={onConfirm}
          className="mt-6 w-full rounded-xl bg-sky-500 px-4 py-3 font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {submitting ? 'Fechando...' : 'Continuar'}
        </button>

        {remaining > 0 && (
          <p className="mt-3 text-xs text-slate-500">
            Mais {remaining} {remaining === 1 ? 'aviso' : 'avisos'} para ver
          </p>
        )}
      </div>
    </div>
  )
}

export default FreezeUseNotice
