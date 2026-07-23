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
      {/* Card escuro com contorno verde, não mais preenchido de verde. O
          destaque vem do anel e do número — suficiente para marcar o momento
          sem que a única tela colorida do app seja um bloco chapado. */}
      <div className="w-full max-w-sm rounded-md bg-surface px-6 py-8 text-center ring-1 ring-brand-green/40">
        <span
          aria-hidden="true"
          className="mx-auto flex size-16 items-center justify-center rounded-full ring-1 ring-brand-green/30"
        >
          <Flame size={30} strokeWidth={1.75} className="text-brand-green" />
        </span>

        <h2
          id="milestone-title"
          className="mt-4 font-display text-3xl font-semibold tracking-tight text-brand-green"
        >
          {milestone.milestone} dias seguidos
        </h2>

        <p className="mt-2 text-sm text-slate-400">
          Média de{' '}
          <span className="font-semibold text-slate-200 tabular-nums">
            {averagePercent}%
          </span>{' '}
          no período.
        </p>

        <button
          type="button"
          autoFocus
          disabled={submitting}
          onClick={onConfirm}
          className="mt-6 w-full rounded-xl bg-brand-green px-4 py-3 font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {submitting ? 'Fechando...' : 'Continuar'}
        </button>

        {remaining > 0 && (
          <p className="mt-3 text-xs text-slate-500">
            Mais {remaining} {remaining === 1 ? 'conquista' : 'conquistas'} para
            ver
          </p>
        )}
      </div>
    </div>
  )
}

export default MilestoneCelebration
