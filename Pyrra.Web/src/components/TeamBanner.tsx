import { TEAM_BANNER_GRADIENTS } from '../utils/teamBanners'
import type { TeamBannerTheme } from '../types/teams'

// imagem customizada tem prioridade sobre o gradiente temático, proporção 4:3 fixa nos dois casos
// title só se aplica com imagem — sobrepõe um degradê escuro pra dar legibilidade ao texto
export function TeamBanner({
  theme,
  imageUrl,
  title,
  className,
}: {
  theme: TeamBannerTheme
  imageUrl?: string | null
  title?: string
  className?: string
}) {
  const classes = ['relative overflow-hidden aspect-[4/3]', className].filter(Boolean).join(' ')

  if (imageUrl) {
    return (
      <div className={classes}>
        <img src={imageUrl} alt="" aria-hidden="true" className="absolute inset-0 size-full object-cover" />
        {/* fade em vez de corte seco, dá contraste pro nome do time ficar legível por cima */}
        <div
          aria-hidden="true"
          className="absolute inset-x-0 bottom-0 h-2/3 bg-gradient-to-t from-black/80 via-black/20 to-transparent"
        />
        {title && (
          <p className="absolute inset-x-0 bottom-0 truncate px-3 py-2 font-display text-sm font-semibold text-ink">
            {title}
          </p>
        )}
      </div>
    )
  }

  return (
    <div className={classes}>
      <div
        aria-hidden="true"
        className={['absolute inset-0 bg-gradient-to-br', TEAM_BANNER_GRADIENTS[theme]].join(' ')}
      />
    </div>
  )
}

export default TeamBanner
