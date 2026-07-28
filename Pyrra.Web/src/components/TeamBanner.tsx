import { TEAM_BANNER_GRADIENTS } from '../utils/teamBanners'
import type { TeamBannerTheme } from '../types/teams'

// Banner — imagem customizada (quando existe) tem prioridade sobre o gradiente temático,
// reaproveitado na lista "Meus Times", no header de Detalhe e nos cards de Explorar. Proporção
// 4:3 fixa nos dois casos (imagem ou cor), pra não ficar a faixa larga/baixa de antes, que
// cortava fotos de forma agressiva.
//
// `title`, quando passado, é sobreposto na base do banner com um degradê escuro por trás — só
// faz sentido pra imagem (dar legibilidade ao texto sobre a foto); no caso de cor sólida não há
// nada "cortando", então o gradiente de cor continua exatamente como antes, sem overlay de texto
// (quem chama continua exibindo o nome por fora, como já fazia).
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
        {/* Fade em vez de corte seco: a foto quase inteira, escurecendo suave na base — e é o que
            dá contraste pro nome do time ficar legível por cima, quando `title` é passado. */}
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
