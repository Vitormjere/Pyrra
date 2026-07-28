import type { TeamBannerTheme } from '../types/teams'

// Cada tema vira um gradiente de baixa opacidade sobre o bg-surface escuro do app — mesma
// linguagem visual discreta do resto do design system, sem upload de imagem. Verde reaproveita
// a cor de marca; os demais usam a escala padrão do Tailwind, já que não há outras cores de marca
// definidas em index.css hoje.
export const TEAM_BANNER_GRADIENTS: Record<TeamBannerTheme, string> = {
  Verde: 'from-brand-green/25 to-brand-green/5',
  Azul: 'from-sky-500/25 to-sky-900/5',
  Roxo: 'from-violet-500/25 to-violet-900/5',
  Laranja: 'from-amber-500/25 to-amber-900/5',
  Vermelho: 'from-rose-500/25 to-rose-900/5',
  Dourado: 'from-yellow-400/25 to-yellow-900/5',
}

export const TEAM_BANNER_THEMES: readonly TeamBannerTheme[] = [
  'Verde',
  'Azul',
  'Roxo',
  'Laranja',
  'Vermelho',
  'Dourado',
]

// Swatch sólido pra tela de criação (mostra a cor em si, não o gradiente translúcido do banner).
export const TEAM_BANNER_SWATCH: Record<TeamBannerTheme, string> = {
  Verde: 'bg-brand-green',
  Azul: 'bg-sky-500',
  Roxo: 'bg-violet-500',
  Laranja: 'bg-amber-500',
  Vermelho: 'bg-rose-500',
  Dourado: 'bg-yellow-400',
}
