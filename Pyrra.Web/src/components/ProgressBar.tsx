interface ProgressBarProps {
  /** Pode passar de 100 (meta já superada) — a barra fica cheia, sem transbordar. */
  percent: number
  className?: string
}

// Barra linear simples pra progresso de metas cumulativas (Fase 5c) — mais compacta que
// ProgressRing, pensada pra caber numa linha de lista junto de texto (ex.: "7 / 10km").
export function ProgressBar({ percent, className }: ProgressBarProps) {
  const safePercent = Math.max(0, Math.min(100, percent))

  return (
    <div
      role="progressbar"
      aria-valuenow={Math.round(safePercent)}
      aria-valuemin={0}
      aria-valuemax={100}
      className={['h-1.5 w-full overflow-hidden rounded-full bg-surface-hi', className].filter(Boolean).join(' ')}
    >
      <div
        className="h-full rounded-full bg-brand-green transition-[width] duration-500"
        style={{ width: `${safePercent}%` }}
      />
    </div>
  )
}

export default ProgressBar
