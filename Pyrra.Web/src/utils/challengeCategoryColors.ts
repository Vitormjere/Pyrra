import type { ChallengeCategoryColor } from '../types/challenges'

// Mesma paleta de utils/teamBanners.ts, arquivo separado de propósito — são domínios diferentes
// (Time vs Categoria de Desafio) mesmo reaproveitando as mesmas 6 cores por consistência visual.
export const CHALLENGE_CATEGORY_SWATCH: Record<ChallengeCategoryColor, string> = {
  Verde: 'bg-brand-green',
  Azul: 'bg-sky-500',
  Roxo: 'bg-violet-500',
  Laranja: 'bg-amber-500',
  Vermelho: 'bg-rose-500',
  Dourado: 'bg-yellow-400',
}

export const CHALLENGE_CATEGORY_COLORS: readonly ChallengeCategoryColor[] = [
  'Verde',
  'Azul',
  'Roxo',
  'Laranja',
  'Vermelho',
  'Dourado',
]

export const CHALLENGE_CATEGORY_TEXT: Record<ChallengeCategoryColor, string> = {
  Verde: 'text-brand-green',
  Azul: 'text-sky-400',
  Roxo: 'text-violet-400',
  Laranja: 'text-amber-400',
  Vermelho: 'text-rose-400',
  Dourado: 'text-yellow-400',
}
