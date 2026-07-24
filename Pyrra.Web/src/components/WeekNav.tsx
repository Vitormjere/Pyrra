import { ChevronLeft, ChevronRight } from 'lucide-react'
import { formatShortDate } from '../utils/format'

interface WeekNavProps {
  /** Segunda-feira da semana visível, "YYYY-MM-DD". */
  weekStart: string
  /** Domingo da semana visível, "YYYY-MM-DD". */
  weekEnd: string
  /** Trava a seta "próxima" na semana atual — não faz sentido consultar o futuro. */
  canGoNext: boolean
  /** Desabilita as duas setas enquanto uma troca de semana está em voo. */
  busy?: boolean
  onPrev: () => void
  onNext: () => void
}

// Navegação anterior/próxima semana, com o intervalo visível no meio. Usada
// idêntica em Tarefas e Finanças para as duas telas navegarem igual.
export function WeekNav({
  weekStart,
  weekEnd,
  canGoNext,
  busy = false,
  onPrev,
  onNext,
}: WeekNavProps) {
  return (
    <div className="flex items-center justify-between gap-2">
      <button
        type="button"
        onClick={onPrev}
        disabled={busy}
        aria-label="Semana anterior"
        className="rounded-lg p-1.5 text-slate-400 transition hover:bg-surface-hi hover:text-slate-200 disabled:opacity-40"
      >
        <ChevronLeft size={18} />
      </button>

      <span className="text-sm font-medium text-slate-300 tabular-nums">
        {formatShortDate(weekStart)} – {formatShortDate(weekEnd)}
      </span>

      <button
        type="button"
        onClick={onNext}
        disabled={busy || !canGoNext}
        aria-label="Próxima semana"
        className="rounded-lg p-1.5 text-slate-400 transition hover:bg-surface-hi hover:text-slate-200 disabled:opacity-40"
      >
        <ChevronRight size={18} />
      </button>
    </div>
  )
}

export default WeekNav
