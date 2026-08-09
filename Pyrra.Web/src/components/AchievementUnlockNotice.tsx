import { useEffect } from 'react'
import { classesForRarity, iconForAchievementType, RARITY_LABELS } from '../utils/achievementDisplay'
import type { PendingAchievementResponse } from '../types/achievement'

interface AchievementUnlockNoticeProps {
  achievement: PendingAchievementResponse
  /** Quantos desbloqueios ainda faltam depois deste, para mostrar o progresso da fila. */
  remaining: number
  submitting: boolean
  onConfirm: () => void
}

// mesma casca do MilestoneCelebration/FreezeUseNotice — cor pela raridade da conquista em vez de fixa
export function AchievementUnlockNotice({
  achievement,
  remaining,
  submitting,
  onConfirm,
}: AchievementUnlockNoticeProps) {
  // esc fecha, como nos outros dois modais de celebração
  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        onConfirm()
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [onConfirm])

  const classes = classesForRarity(achievement.rarity)
  const Icon = iconForAchievementType(achievement.type)

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-brand-dark/80 p-4 backdrop-blur-sm"
      role="dialog"
      aria-modal="true"
      aria-labelledby="achievement-title"
    >
      <div className={`w-full max-w-sm rounded-md bg-surface px-6 py-8 text-center ring-1 ${classes.ring}`}>
        <span
          aria-hidden="true"
          className={`mx-auto flex size-16 items-center justify-center rounded-full ring-1 ${classes.ringSoft}`}
        >
          <Icon size={30} strokeWidth={1.75} className={classes.text} />
        </span>

        {achievement.rarity && (
          <p className={`mt-3 text-xs font-semibold tracking-wide uppercase ${classes.text}`}>
            {RARITY_LABELS[achievement.rarity]}
          </p>
        )}

        <h2
          id="achievement-title"
          className={`mt-1 font-display text-2xl font-semibold tracking-tight ${classes.text}`}
        >
          {achievement.name}
        </h2>

        <p className="mt-2 text-sm text-slate-400">{achievement.description}</p>

        <p className="mt-3 text-sm font-semibold text-slate-200 tabular-nums">+{achievement.xp} XP</p>

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
            Mais {remaining} {remaining === 1 ? 'conquista' : 'conquistas'} para ver
          </p>
        )}
      </div>
    </div>
  )
}

export default AchievementUnlockNotice
