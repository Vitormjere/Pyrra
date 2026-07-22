import { useEffect } from 'react'
import { Flame } from 'lucide-react'
import type { PendingMilestoneResponse } from '../types/streak'

interface MilestoneCelebrationProps {
  milestone: PendingMilestoneResponse
  /** Quantos marcos ainda faltam depois deste, para mostrar o progresso da fila. */
  remaining: number
  submitting: boolean
  onConfirm: () => void
}

// Verde sólido de propósito: é a mesma regra do card do foguinho quando a meta é
// batida — preenchimento pleno em brand-green fica reservado a ação e conquista,
// e um marco de streak é o ápice da segunda categoria.
export function MilestoneCelebration({
  milestone,
  remaining,
  submitting,
  onConfirm,
}: MilestoneCelebrationProps) {
  // Esc fecha, como em qualquer diálogo. Sem isso o modal vira uma parede para
  // quem navega por teclado.
  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        onConfirm()
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [onConfirm])

  const averagePercent = Math.round(milestone.averagePercentage * 100)

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-brand-dark/80 p-4 backdrop-blur-sm"
      role="dialog"
      aria-modal="true"
      aria-labelledby="milestone-title"
    >
      <div className="w-full max-w-sm rounded-2xl bg-brand-green px-6 py-8 text-center text-brand-dark">
        <Flame
          size={56}
          strokeWidth={2}
          aria-hidden="true"
          className="mx-auto"
        />

        <h2
          id="milestone-title"
          className="mt-4 font-display text-3xl tracking-tight"
        >
          {milestone.milestone} dias seguidos!
        </h2>

        <p className="mt-2 text-sm text-brand-dark/70">
          Média de <span className="font-semibold tabular-nums">{averagePercent}%</span>{' '}
          no período.
        </p>

        <button
          type="button"
          autoFocus
          disabled={submitting}
          onClick={onConfirm}
          className="mt-6 w-full rounded-xl bg-brand-dark px-4 py-3 font-semibold text-brand-green transition hover:brightness-125 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {submitting ? 'Fechando...' : 'Continuar'}
        </button>

        {remaining > 0 && (
          <p className="mt-3 text-xs text-brand-dark/60">
            Mais {remaining} {remaining === 1 ? 'conquista' : 'conquistas'} para
            ver
          </p>
        )}
      </div>
    </div>
  )
}

export default MilestoneCelebration
