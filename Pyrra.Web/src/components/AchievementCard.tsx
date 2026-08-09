import { Lock } from 'lucide-react'
import ProgressBar from './ProgressBar'
import { classesForRarity, iconForAchievementType, progressUnitLabel, RARITY_LABELS } from '../utils/achievementDisplay'
import { formatShortDate } from '../utils/format'
import type { AchievementResponse } from '../types/achievement'

interface AchievementCardProps {
  achievement: AchievementResponse
  /** Só chamado em conquistas desbloqueadas — bloqueadas não têm o que compartilhar. */
  onClick?: () => void
}

// grade do perfil: desbloqueada mostra ícone colorido pela raridade, XP e data; bloqueada aparece apagada com cadeado e progresso, se der pra calcular
export function AchievementCard({ achievement, onClick }: AchievementCardProps) {
  const Icon = iconForAchievementType(achievement.type)
  const classes = classesForRarity(achievement.rarity)
  const clickable = achievement.unlocked && onClick !== undefined

  const percent =
    achievement.currentProgress !== null
      ? Math.min(100, Math.round((achievement.currentProgress / achievement.milestone) * 100))
      : 0

  const Wrapper = clickable ? 'button' : 'div'

  return (
    <Wrapper
      type={clickable ? 'button' : undefined}
      onClick={clickable ? onClick : undefined}
      className={[
        'flex flex-col items-center gap-1.5 rounded-md px-3 py-4 text-center ring-1 transition',
        achievement.unlocked
          ? `bg-surface ${classes.ring} ${clickable ? 'cursor-pointer hover:bg-surface-hi' : ''}`
          : 'bg-surface/60 ring-line',
      ].join(' ')}
    >
      <span
        aria-hidden="true"
        className={[
          'relative flex size-12 items-center justify-center rounded-full ring-1',
          achievement.unlocked ? classes.ringSoft : 'ring-line',
        ].join(' ')}
      >
        <Icon size={22} strokeWidth={1.75} className={achievement.unlocked ? classes.text : 'text-slate-700'} />
        {!achievement.unlocked && (
          <span className="absolute -right-1 -bottom-1 flex size-5 items-center justify-center rounded-full bg-surface-hi ring-1 ring-line">
            <Lock size={11} className="text-slate-500" />
          </span>
        )}
      </span>

      <p
        className={[
          'line-clamp-2 text-xs font-semibold',
          achievement.unlocked ? 'text-ink' : 'text-slate-500',
        ].join(' ')}
      >
        {achievement.name}
      </p>

      {achievement.unlocked ? (
        <>
          {achievement.rarity && (
            <p className={`text-[10px] font-medium tracking-wide uppercase ${classes.text}`}>
              {RARITY_LABELS[achievement.rarity]}
            </p>
          )}
          <p className="text-[11px] text-slate-500 tabular-nums">
            +{achievement.xp} XP
            {achievement.unlockedAt && ` · ${formatShortDate(achievement.unlockedAt.slice(0, 10))}`}
          </p>
        </>
      ) : (
        <div className="w-full">
          <ProgressBar percent={percent} />
          {achievement.currentProgress !== null && (
            <p className="mt-1 text-[10px] text-slate-600 tabular-nums">
              {achievement.currentProgress} de {achievement.milestone} {progressUnitLabel(achievement.type)}
            </p>
          )}
        </div>
      )}
    </Wrapper>
  )
}

export default AchievementCard
